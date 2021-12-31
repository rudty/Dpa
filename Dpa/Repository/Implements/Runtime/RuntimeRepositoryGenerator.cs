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
        private static readonly Dictionary<(Type, Type), Type> typeGenerateCache = new Dictionary<(Type, Type), Type>();
        private static readonly object typeCacheLock = new object();
        private static int generateCount = 0;

        public static Type Generate(Type baseType, Type generateInterface)
        {
            Type buildType;
            lock (typeCacheLock)
            {
                if (typeGenerateCache.TryGetValue((baseType, generateInterface), out buildType))
                {
                    return buildType;
                }
            }

            int gen = Interlocked.Increment(ref generateCount);

            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                  "Custom_generate" + gen,
                  TypeAttributes.Public | TypeAttributes.Class,
                  parent: baseType,
                  interfaces: new Type[] { generateInterface });

            GenerateConstructor(typeBuilder, baseType);
            GenerateMethod(typeBuilder, baseType, generateInterface);
            buildType = typeBuilder.CreateType();

            lock (typeCacheLock)
            {
                typeGenerateCache.TryAdd((baseType, generateInterface), buildType);
            }

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
        public static void GenerateMethod(TypeBuilder typeBuilder, Type baseType, Type interfaceType)
        {
            Type dictionaryType = typeof(Dictionary<string, object>);
            ConstructorInfo dictionaryCtor = dictionaryType.GetConstructor(new Type[0]);
            MethodInfo dictionaryAddMethod = dictionaryType.GetMethod("Add");
            FieldInfo connectionField = baseType.GetField("connection", BindingFlags.NonPublic | BindingFlags.Instance);
            Type myType = typeof(RuntimeRepositoryGenerator);

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

                MethodBuilder methodBuilder = typeBuilder.DefineMethod(method.Name,
                    MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.NewSlot,
                    methodReturnType,
                    methodParamTypes);

                ILGenerator il = methodBuilder.GetILGenerator(256);

                // var dict = new Dictionary();
                LocalBuilder localDictionary = il.DeclareLocal(dictionaryType);
                il.Emit(OpCodes.Newobj, dictionaryCtor);
                il.Emit(OpCodes.Stloc, localDictionary);

                ParameterInfo[] parameters = method.GetParameters();
                /***
                 * foreach (var p in parameters) {
                 *  dictionary.Add(p.Name, value);
                 * }
                 */
                for (int i = 0; i < parameters.Length; ++i)
                {
                    /* dict.Add(string, value);*/
                    il.Emit(OpCodes.Ldloc, localDictionary); // this
                    il.Emit(OpCodes.Ldstr, parameters[i].Name); // string
                    il.Emit(OpCodes.Ldarg, i + 1);
                    if (parameters[i].ParameterType.IsValueType)
                    {
                        // (object) value
                        il.Emit(OpCodes.Box, parameters[i].ParameterType);
                    }
                    il.Emit(OpCodes.Call, dictionaryAddMethod); // add
                }

                MethodInfo callMethod;
                if (methodReturnType == typeof(Task))
                {
                    callMethod = myType.GetMethod("DapperExecute", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);
                }
                else
                {
                    Type taskReturnType = methodReturnType.GetGenericArguments()[0];
                    if (taskReturnType.IsGenericType)
                    {
                        Type firstGenericArgmentType = taskReturnType.GetGenericArguments()[0];
                        if (!typeof(System.Collections.Generic.IEnumerable<>).MakeGenericType(firstGenericArgmentType).IsAssignableFrom(taskReturnType))
                        {
                            throw new ArgumentException("must IEnumerable");
                        }
                        callMethod = myType.GetMethod("DapperQuery", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance)
                            .MakeGenericMethod(firstGenericArgmentType);
                    }
                    else
                    {
                        callMethod = myType.GetMethod("DapperQueryFirst", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance)
                            .MakeGenericMethod(taskReturnType);
                    }
                }

                //DapperExecute(this, sql, param, ommandType)
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, connectionField);
                il.Emit(OpCodes.Ldstr, sqlQuery);
                il.Emit(OpCodes.Ldloc, localDictionary);
                il.Emit(OpCodes.Ldc_I4, (int)commandType);
                il.Emit(OpCodes.Call, callMethod);

                il.Emit(OpCodes.Ret);
            }
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
            return Dapper.SqlMapper.QueryFirstAsync<E>(connection, sql, param, commandType: commandType);
        }
    }
}
