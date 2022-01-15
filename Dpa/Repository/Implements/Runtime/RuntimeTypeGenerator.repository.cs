using Dpa.Repository.Implements.Types;
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
            int gen = Interlocked.Increment(ref generateCount);

            TypeBuilder typeBuilder = moduleBuilder.DefineType(
                  "Custom_generate" + gen,
                  TypeAttributes.Public | TypeAttributes.Class,
                  parent: baseType,
                  interfaces: new Type[] { generateInterface });

            GenerateConstructor(typeBuilder, baseType);
            GenerateMethod(typeBuilder, generateInterface);

            Type[] baseInterfaces = generateInterface.GetInterfaces();
            Type crudType = typeof(ICrudRepository<,>);
            foreach (Type baseInterface in baseInterfaces)
            {
                if (baseInterface.IsGenericType)
                {
                    Type[] argType = baseInterface.GetGenericArguments();
                    if (argType.Length == 2)
                    {
                        if (crudType.MakeGenericType(argType).IsAssignableFrom(baseInterface))
                        {
                            continue;
                        }
                    }
                }

                GenerateMethod(typeBuilder, baseInterface);
            }

            Type buildType = typeBuilder.CreateType();

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
            string interfaceName = ReflectUtils.GetFullName(interfaceType);

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

                MethodBuilder methodBuilder = typeBuilder.DefineMethod(interfaceName + "." + method.Name,
                    MethodAttributes.Private | MethodAttributes.HideBySig |
                    MethodAttributes.NewSlot | MethodAttributes.Virtual |
                    MethodAttributes.Final,
                    methodReturnType,
                    methodParamTypes);

                typeBuilder.DefineMethodOverride(methodBuilder, method);

                ILGenerator il = methodBuilder.GetILGenerator(256);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, connectionField);
                il.Emit(OpCodes.Ldstr, sqlQuery); 
                il.Emit(OpCodes.Ldc_I4, (int)commandType);

                MethodParameters parameters = new MethodParameters(method);
                if (parameters.TransactionPosition > -1)
                {
                    il.Emit(OpCodes.Ldarg, parameters.TransactionPosition + 1);
                } 
                else
                {
                    il.Emit(OpCodes.Ldnull);
                }

                int entityPosition = parameters.EntityParameterPosition;
                if (entityPosition > -1)
                {
                    // Execute(connection, "select * from table", commandType, (param));
                    il.Emit(OpCodes.Ldarg, entityPosition + 1);

                    Type entityType = parameters[entityPosition].ParameterType;
                    if (commandType == CommandType.StoredProcedure && 
                        ReflectUtils.HasEntityAttribute(entityType))
                    {
                        // Execute(connection, "select * from table", commandType, new {
                        // A = param.A, B = param.B
                        // });
                        Type newType = GenerateAnonymousEntityFromEntity(entityType);
                        ConstructorInfo ctor = newType.GetConstructors()[0];
                        il.Emit(OpCodes.Newobj, ctor);
                    } 
                    else if (entityType.IsValueType)
                    {
                        il.Emit(OpCodes.Box, entityType);
                    }
                }
                else
                {
                    // object param = new { p1 = arg0, p2 = arg1 };
                    // Execute(connection, "select * from table", commandType, param);
                    Type anonymousClassType = GenerateAnonymousEntityFromParameter(parameters.QueryParameters);
                    ConstructorInfo anonymousCtor = anonymousClassType.GetConstructors()[0];
                    for (int i = 0; i < parameters.Length; ++i)
                    {
                        if (i == parameters.TransactionPosition)
                        {
                            continue;
                        }

                        il.Emit(OpCodes.Ldarg, i + 1);
                    }
                    il.Emit(OpCodes.Newobj, anonymousCtor);
                }

                il.Emit(OpCodes.Call, callMethod);
                il.Emit(OpCodes.Ret);
            }
        }

        private struct MethodParameters
        {
            /// <summary>
            /// 전체 파라메터들
            /// </summary>
            public readonly ParameterInfo[] Parameters;

            /// <summary>
            /// IDbTransaction 을 제외한 파라메터들
            /// </summary>
            public readonly ParameterInfo[] QueryParameters;

            /// <summary>
            /// IDbTransaction 시그니처 위치
            /// </summary>
            public readonly int TransactionPosition;

            /// <summary>
            /// Entity 의 시그니처 위치
            /// </summary>
            public readonly int EntityParameterPosition;

            /// <summary>
            /// 시그니처 파라메터 수 (= Parameters.Length)
            /// </summary>
            public readonly int Length;

            public MethodParameters(MethodInfo methodInfo)
            {
                this.Parameters = methodInfo.GetParameters();

                int transactionPosition = -1;
                for (int i = 0; i < Parameters.Length; ++i)
                {
                    if (typeof(IDbTransaction).IsAssignableFrom(Parameters[i].ParameterType))
                    {
                        transactionPosition = i;
                    }
                }
                
                TransactionPosition = transactionPosition;
                Length = Parameters.Length;
                QueryParameters = GetQueryparameters(Parameters, TransactionPosition);
                EntityParameterPosition = GetEntityPosition(Parameters, TransactionPosition);
            }

            public ParameterInfo this[int index]
            {
                get => Parameters[index];
            }

            private static ParameterInfo[] GetQueryparameters(ParameterInfo[] parameters, int transactionPosition)
            {
                if (transactionPosition == -1)
                {
                    return parameters;
                }

                ParameterInfo[] queryParameters = new ParameterInfo[parameters.Length - 1];
                for (int i = 0, p_i = 0; i < parameters.Length; ++i)
                {
                    if (i == transactionPosition)
                    {
                        continue;
                    }

                    queryParameters[p_i] = parameters[i];
                    p_i += 1;
                }
                return queryParameters;
            }

            private static int GetEntityPosition(ParameterInfo[] parameters, int transactionPosition)
            {
                int maybeEntityPosition;
                switch (parameters.Length)
                {
                    default:
                        return -1;    
                    case 1:
                        if (transactionPosition > 0)
                        {
                            /* func(IDbTransaction tran)*/
                            return -1;
                        }
                        else
                        {
                            /* func(Entity entity)*/
                            maybeEntityPosition = 0;
                        }
                        break;
                    case 2:
                        if (transactionPosition == -1)
                        {
                            /* func(Entity entity, Entity2 entity)*/
                            return -1;
                        }
                        else if (transactionPosition == 0)
                        {
                            /* func(IDbTransaction tran, Entity entity)*/
                            maybeEntityPosition = 1;
                        }
                        else
                        {
                            maybeEntityPosition = 0;
                        }
                        break;
                }

                if (ReflectUtils.IsPrimitiveLike(parameters[maybeEntityPosition].ParameterType))
                {
                    return -1;
                }

                return maybeEntityPosition;
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
                    TypeMapper.SetType(firstGenericArgmentType);
                    return dapperQueryMethod.MakeGenericMethod(firstGenericArgmentType);
                }
            }

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(taskResultType))
            {
                return dapperQueryMethod_object;
            }

            TypeMapper.SetType(taskResultType);
            return dapperQueryFirstMethod.MakeGenericMethod(taskResultType);
        }

        public static Task DapperExecute(DbConnection connection, string sql, System.Data.CommandType commandType, IDbTransaction transaction, object param)
        {
            return Dapper.SqlMapper.ExecuteAsync(connection, sql, param, transaction, commandType: commandType);
        }

        public static Task<IEnumerable<E>> DapperQuery<E>(DbConnection connection, string sql, System.Data.CommandType commandType, IDbTransaction transaction, object param)
        {
            return Dapper.SqlMapper.QueryAsync<E>(connection, sql, param, transaction, commandType: commandType);
        }

        public static Task<E> DapperQueryFirst<E>(DbConnection connection, string sql, System.Data.CommandType commandType, IDbTransaction transaction, object param)
        {
            return Dapper.SqlMapper.QueryFirstOrDefaultAsync<E>(connection, sql, param, transaction, commandType: commandType);
        }
    }
}
