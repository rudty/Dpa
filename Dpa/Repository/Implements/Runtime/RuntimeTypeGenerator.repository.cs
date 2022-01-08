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
    public static partial class RuntimeTypeGenerator
    {
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
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final,
                    methodReturnType,
                    methodParamTypes);

                ILGenerator il = methodBuilder.GetILGenerator(256);
                ParameterInfo[] parameters = method.GetParameters();
                if (IsEntityParameter(parameters))
                {
                    Type entityType = parameters[0].ParameterType;
                    // Execute(connection, "select * from table", (param), commandType);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldfld, connectionField);
                    il.Emit(OpCodes.Ldstr, sqlQuery);
                    il.Emit(OpCodes.Ldarg_1);

                    if (ReflectUtils.HasEntityAttribute(entityType))
                    {
                        // Execute(connection, "select * from table", new {
                        // A = param.A, B = param.B
                        // }, commandType);
                        Type newType = GenerateAnonymousEntityFromEntity(entityType);
                        ConstructorInfo ctor = newType.GetConstructors()[0];
                        il.Emit(OpCodes.Newobj, ctor);
                    }

                    il.Emit(OpCodes.Ldc_I4, (int)commandType);
                    il.Emit(OpCodes.Call, callMethod);

                    il.Emit(OpCodes.Ret);
                }
                else
                {
                    // object param = new { p1 = arg0, p2 = arg1 };
                    // Execute(connection, "select * from table", param, commandType);
                    Type anonymousClassType = GenerateAnonymousEntityFromParameter(parameters);
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
        private static readonly Type myType = typeof(RuntimeTypeGenerator);
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
                    ReflectUtils.SetTypeMap(firstGenericArgmentType);
                    return dapperQueryMethod.MakeGenericMethod(firstGenericArgmentType);
                }
            }

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(taskResultType))
            {
                return dapperQueryMethod_object;
            }

            ReflectUtils.SetTypeMap(taskResultType);
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

            if (ReflectUtils.IsPrimitiveLike(firstType))
            {
                return false;
            }
            return true;
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
