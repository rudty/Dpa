using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
namespace Dpa.Repository.Implements.Runtime
{
    public class RuntimeRepositoryGenerator
    {
        private static readonly AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("repo_assembly"), AssemblyBuilderAccess.Run);
        private static readonly ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("repoModule");

        private static int generateCount = 0;

        public static Type Generate(Type baseType, Type generateInterface)
        {
            Type buildType;
  
            int gen = Interlocked.Increment(ref generateCount);

            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                  "Custom_generate" + gen,
                  TypeAttributes.Public | TypeAttributes.Class,
                  parent: baseType,
                  interfaces: new Type[] { generateInterface });

            GenerateConstructor(typeBuilder, baseType);
            GenerateMethod(typeBuilder, generateInterface);
            buildType = typeBuilder.CreateType();

            return buildType;
        }

        /// <summary>
        ///  class Anonymous_generate_1 {
        ///     public int m_a;
        ///     public int m_b;
        ///     
        ///     public int a { get { return this.m_a } }
        ///     public int b { get { return this.m_b } }
        ///
        ///     public Anonymous_generate_1(int a, int b) {
        ///         this.m_a = a;
        ///         this.m_b = b;
        ///     }
        ///  }
        /// </summary>
        private static Type GenerateParameterAnonymousEntity(ParameterInfo[] parameters)
        {
            const string memberPrefix = "m_";
            const string getterMethodPrefix = "get_";
            int gen = Interlocked.Increment(ref generateCount);

            TypeBuilder typeBuilder = moduleBuilder.DefineType("Anonymous_generate" + gen);
            FieldBuilder[] fields = new FieldBuilder[parameters.Length];

            for (int i = 0; i < parameters.Length; ++i)
            {
                // int m_value;
                // int value { get; }
                // int get_value() { return this.m_value; } <- 이게 위에 프로퍼티에서 get; 

                fields[i] = typeBuilder.DefineField(
                    memberPrefix + parameters[i].Name, 
                    parameters[i].ParameterType, 
                    FieldAttributes.Public);

                PropertyBuilder property = typeBuilder.DefineProperty(
                    parameters[i].Name,
                    PropertyAttributes.HasDefault,
                    parameters[i].ParameterType,
                    null);

                MethodBuilder propertyGetter = typeBuilder.DefineMethod(
                    getterMethodPrefix + parameters[i].Name, 
                    MethodAttributes.Public, 
                    parameters[i].ParameterType, 
                    null);

                ILGenerator gil = propertyGetter.GetILGenerator();
                gil.Emit(OpCodes.Ldarg_0);
                gil.Emit(OpCodes.Ldfld, fields[i]);
                gil.Emit(OpCodes.Ret);

                property.SetGetMethod(propertyGetter);
            }

            ConstructorBuilder ctor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard, 
                parameters.Select(e => e.ParameterType).ToArray());


            ILGenerator il = ctor.GetILGenerator();

            for (int i = 0; i < parameters.Length; ++i)
            {
                // this.field[i] = arg[i]
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg, i + 1);
                il.Emit(OpCodes.Stfld, fields[i]);
            }

            il.Emit(OpCodes.Ret);

            for (int i = 0; i < parameters.Length; ++i)
            {
                ctor.DefineParameter(i + 1, ParameterAttributes.In, parameters[i].Name);
            }

            return typeBuilder.CreateType();
        }


        /// <summary>
        /// call base(this, arg1, ...);
        /// </summary>
        private static void GenerateConstructor(TypeBuilder typeBuilder, Type baseType)
        {
            ConstructorInfo baseCtor = baseType.GetConstructors()[0];
            Type[] ctorParams = baseCtor.GetParameters().Select(e => e.ParameterType).ToArray();
            ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, ctorParams);
            ILGenerator il = ctorBuilder.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            for (int i = 0; i < ctorParams.Length; ++i)
            {
                il.Emit(OpCodes.Ldarg, i + 1);
            }
            il.Emit(OpCodes.Call, baseCtor);
            il.Emit(OpCodes.Ret);
        }

        /// <summary>
        /// interface의 멤버들을 구현합니다
        /// </summary>
        public static void GenerateMethod(TypeBuilder typeBuilder, Type interfaceType)
        {
            FieldInfo connectionField = BaseRepository.ConnectionField;

            foreach (var method in interfaceType.GetMethods())
            {
                Type[] methodParamTypes = method.GetParameters().Select(e => e.ParameterType).ToArray();
                Type methodReturnType = method.ReturnType;
                QueryAttribute queryAttribute = method.GetCustomAttribute<QueryAttribute>();

                string sqlQuery;
                CommandType commandType;
                if (queryAttribute is null)
                {
                    sqlQuery = method.Name;
                    commandType = CommandType.StoredProcedure;
                }
                else
                {
                    sqlQuery = queryAttribute.Query;
                    commandType = queryAttribute.CommandType;
                }

                MethodInfo callMethod = FindCallMethod(methodReturnType);

                MethodBuilder methodBuilder = typeBuilder.DefineMethod(method.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot,
                    methodReturnType,
                    methodParamTypes);

                ILGenerator il = methodBuilder.GetILGenerator(256);
                ParameterInfo[] parameters = method.GetParameters();
                if (IsEntityParameter(parameters))
                {
                    // Execute(connection, "select * from table", (param), commandType);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, connectionField);
                    il.Emit(OpCodes.Ldstr, sqlQuery);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, (int)commandType);
                    il.Emit(OpCodes.Call, callMethod);

                    il.Emit(OpCodes.Ret);
                }
                else
                {
                    // object param = new { p1 = arg0, p2 = arg1 };
                    // Execute(connection, "select * from table", param, commandType);
                    Type anonymousClassType = GenerateParameterAnonymousEntity(parameters);
                    ConstructorInfo anonymousCtor = anonymousClassType.GetConstructors()[0];

                    LocalBuilder localAnonymousClass = il.DeclareLocal(anonymousClassType);
                    for (int i = 0; i < parameters.Length; ++i)
                    {
                        il.Emit(OpCodes.Ldarg, i + 1);
                    }
                    il.Emit(OpCodes.Newobj, anonymousCtor);
                    il.Emit(OpCodes.Stloc, localAnonymousClass);

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, connectionField);
                    il.Emit(OpCodes.Ldstr, sqlQuery);
                    il.Emit(OpCodes.Ldloc, localAnonymousClass);
                    il.Emit(OpCodes.Ldc_I4, (int)commandType);
                    il.Emit(OpCodes.Call, callMethod);

                    il.Emit(OpCodes.Ret); 
                }
            }
        }

        private const BindingFlags findMethodFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly Type myType = typeof(RuntimeRepositoryGenerator);
        private static readonly MethodInfo dapperExecuteMethod = myType.GetMethod("DapperExecute", findMethodFlags);
        private static readonly MethodInfo dapperQueryMethod = myType.GetMethod("DapperQuery", findMethodFlags);
        private static readonly MethodInfo dapperQueryMethod_object = dapperQueryMethod.MakeGenericMethod(typeof(object));
        private static readonly MethodInfo dapperQueryFirstMethod = myType.GetMethod("DapperQueryFirst", findMethodFlags);

        private static MethodInfo FindCallMethod(Type t)
        {
            if (t == typeof(Task))
            {
                return dapperExecuteMethod;
            }

            // Task<T>
            Type taskResultType = t.GetGenericArguments()[0];
            if (taskResultType.IsGenericType)
            {
                Type firstGenericArgmentType = taskResultType.GetGenericArguments()[0];
                if (typeof(System.Collections.Generic.IEnumerable<>)
                    .MakeGenericType(firstGenericArgmentType)
                    .IsAssignableFrom(taskResultType))
                {
                    return dapperQueryMethod.MakeGenericMethod(firstGenericArgmentType);
                }
            }

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(taskResultType))
            {
                return dapperQueryMethod_object;
            }

            return dapperQueryFirstMethod.MakeGenericMethod(taskResultType);
        }

        private static bool IsEntityParameter(ParameterInfo[] parameters)
        {
            if (parameters.Length != 1)
            {
                return false;
            }

            Type firstType = parameters[0].ParameterType;

            if (firstType.IsValueType)
            {
                return false;
            }

            if (IsPrimitiveLike(firstType))
            {
                return false;
            }
            return true;
        }

        private static bool IsPrimitiveLike(Type t)
        {
            if (t.IsPrimitive)
            {
                return true;
            }
        
            if (t == typeof(string))
            {
                return true;
            }

            if (t == typeof(DateTime))
            {
                return true;
            }

            if (t == typeof(TimeSpan))
            {
                return true;
            }

            if (t == typeof(System.Numerics.BigInteger))
            {
                return true;
            }

            if (t == typeof(decimal))
            {
                return true;
            }

            return false;
        }

        public static Task DapperExecute(DbConnection connection, string sql, object param, System.Data.CommandType commandType)
        {
            return Dapper.SqlMapper.ExecuteAsync(connection, sql, param, commandType: commandType);
        }

        public static Task<IEnumerable<E>> DapperQuery<E>(DbConnection connection, string sql, object param, System.Data.CommandType commandType)
        {
            return Dapper.SqlMapper.QueryAsync<E>(connection, sql, param, commandType: commandType);
        }

        public static Task<E> DapperQueryFirst<E>(DbConnection connection, string sql, object param, System.Data.CommandType commandType)
        {
            return Dapper.SqlMapper.QueryFirstOrDefaultAsync<E>(connection, sql, param, commandType: commandType);
        }
    }
}
