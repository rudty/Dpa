using Dpa.Repository.Implements.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

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

                MethodInfo callMethod = DapperMethod.FindCallMethod(methodReturnType);

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
                if (parameters.TransactionParameter.HasValue)
                {
                    il.Emit(OpCodes.Ldarg, parameters.TransactionParameter.Value.Info.Position + 1);
                } 
                else
                {
                    il.Emit(OpCodes.Ldnull);
                }

                Column<ParameterInfo>? entityParameter = parameters.EntityParameter;
                if (entityParameter.HasValue)
                {
                    // Execute(connection, "select * from table", commandType, (param));
                    il.Emit(OpCodes.Ldarg, entityParameter.Value.Info.Position + 1);

                    Type entityType = entityParameter.Value.MemberType;
                    if (commandType == CommandType.StoredProcedure)
                    //if (commandType == CommandType.StoredProcedure && 
                    //    ReflectUtils.HasEntityAttribute(entityType))
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
                    foreach (Column<ParameterInfo> queryParameter in parameters.QueryParameters)
                    {
                        il.Emit(OpCodes.Ldarg, queryParameter.Info.Position + 1);
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
            public readonly Entity<ParameterInfo> Parameters;

            /// <summary>
            /// IDbTransaction 을 제외한 파라메터들
            /// </summary>
            public readonly Entity<ParameterInfo> QueryParameters;

            /// <summary>
            /// IDbTransaction 파라메터
            /// </summary>
            public readonly Column<ParameterInfo>? TransactionParameter;

            /// <summary>
            /// Entity 의 파라메터
            /// </summary>
            public readonly Column<ParameterInfo>? EntityParameter;

            /// <summary>
            /// 시그니처 파라메터 수 (= Parameters.Length)
            /// </summary>
            public readonly int Length;

            public MethodParameters(MethodInfo methodInfo)
            {
                Entity<ParameterInfo> parameters = methodInfo.GetMappingParameters();
                this.QueryParameters = new Entity<ParameterInfo>(parameters.Count);
                this.Parameters = new Entity<ParameterInfo>(parameters.Count);

                Column<ParameterInfo>? transactionParameter = null;
                foreach (Column<ParameterInfo> parameterInfo in parameters)
                {
                    Parameters.Add(parameterInfo);

                    if (typeof(IDbTransaction).IsAssignableFrom(parameterInfo.MemberType))
                    {
                        if (transactionParameter != null)
                        {
                            throw new ArgumentException("number of transaction parameters must be 1");
                        } 

                        transactionParameter = parameterInfo;
                    }
                    else
                    {
                        QueryParameters.Add(parameterInfo);
                    }
                }
                
                TransactionParameter = transactionParameter;
                Length = parameters.Count;

                if (QueryParameters.Count == 1 && 
                    false == ReflectUtils.IsDbTypeExists(QueryParameters[0].MemberType))
                {
                    EntityParameter = QueryParameters[0];
                }
                else
                {
                    EntityParameter = null;
                }
            }

            public Column<ParameterInfo> this[int index]
            {
                get => Parameters[index];
            }
        }
    }
}
