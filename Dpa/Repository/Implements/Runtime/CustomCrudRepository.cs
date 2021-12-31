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
    public class CustomCrudRepository<T, ID> : DefaultCrudRepository<T, ID>
    {
        private static readonly AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("repo_assembly"), AssemblyBuilderAccess.Run);
        private static readonly ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("repoModule");
        private static readonly Dictionary<Type, Type> typeGenerateCache = new Dictionary<Type, Type>();
        private static readonly object typeCacheLock = new object();
        private static int generateCount = 0;

        public CustomCrudRepository(DbConnection connection, IRepositoryQuery<T, ID> repositoryQuery) : base(connection, repositoryQuery)
        {
        }

        public static Type Generate(Type generateInterface)
        {
            Type buildType;
            lock (typeCacheLock)
            {
                if (typeGenerateCache.TryGetValue(generateInterface, out buildType))
                {
                    return buildType;
                }
            }

            int gen = Interlocked.Increment(ref generateCount);
            Type baseType = typeof(CustomCrudRepository<T, ID>);

            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                  "CustomRepository_generate" + gen,
                  TypeAttributes.Public | TypeAttributes.Class,
                  parent: baseType,
                  interfaces: new Type[] { generateInterface });

            GenerateConstructor(typeBuilder, baseType);
            GenerateMethod(typeBuilder, baseType, generateInterface);
            buildType = typeBuilder.CreateType();

            lock (typeCacheLock)
            {
                typeGenerateCache.TryAdd(generateInterface, buildType);
            }

            return buildType;
        }

        /// <summary>
        /// call base(this, arg1, arg2);
        /// </summary>
        private static void GenerateConstructor(TypeBuilder typeBuilder, Type baseType)
        {
            Type[] ctorParams = new Type[] { typeof(DbConnection), typeof(IRepositoryQuery<T, ID>) };
            ConstructorBuilder ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, ctorParams);
            ConstructorInfo baseCtor = baseType.GetConstructor(ctorParams);
            ILGenerator il = ctorBuilder.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
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
                    callMethod = baseType.GetMethod("DapperExecute", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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
                        callMethod = baseType.GetMethod("DapperQuery", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    }
                    else
                    {
                        callMethod = baseType.GetMethod("DapperQueryFirst", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    }
                }

                //DapperExecute(this, sql, param, ommandType)
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldstr, sqlQuery);
                il.Emit(OpCodes.Ldloc, localDictionary);
                il.Emit(OpCodes.Ldc_I4, (int)commandType);
                il.Emit(OpCodes.Call, callMethod);

                il.Emit(OpCodes.Ret);
            }
        }

        public Task DapperExecute(string sql, object param, System.Data.CommandType commandType)
        {
            return Dapper.SqlMapper.ExecuteAsync(connection, sql, param, commandType: commandType);
        }

        public Task<IEnumerable<T>> DapperQuery(string sql, object param, System.Data.CommandType commandType)
        {
            return Dapper.SqlMapper.QueryAsync<T>(connection, sql, param, commandType: commandType);
        }

        public Task<T> DapperQueryFirst(string sql, object param, System.Data.CommandType commandType)
        {
            return Dapper.SqlMapper.QueryFirstAsync<T>(connection, sql, param, commandType: commandType);
        }
    }
}
