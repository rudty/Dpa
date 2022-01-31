using Dpa.Repository.Implements.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;

namespace Dpa.Repository.Implements.Runtime
{
    public class DapperMethod
    {
        private const BindingFlags findMethodFlags = BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly Type myType = typeof(DapperMethod);
        private static readonly MethodInfo dapperExecuteMethod = myType.GetMethod("DapperExecute", findMethodFlags);
        private static readonly MethodInfo dapperQueryMethod = myType.GetMethod("DapperQuery", findMethodFlags);
        private static readonly MethodInfo dapperQueryMethod_object = dapperQueryMethod.MakeGenericMethod(typeof(object));
        private static readonly MethodInfo dapperQueryFirstMethod = myType.GetMethod("DapperQueryFirst", findMethodFlags);

        public static readonly MethodInfo dapperAsTableValuedParameter = myType.GetMethod("DapperAsTableValuedParameter", findMethodFlags);

        public static MethodInfo FindCallMethod(Type t)
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
                    TypeMap.SetType(firstGenericArgmentType);
                    return dapperQueryMethod.MakeGenericMethod(firstGenericArgmentType);
                }
            }

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(taskResultType))
            {
                return dapperQueryMethod_object;
            }

            TypeMap.SetType(taskResultType);
            return dapperQueryFirstMethod.MakeGenericMethod(taskResultType);
        }

        public static Task DapperExecute(DbConnection connection, string sql, CommandType commandType, IDbTransaction transaction, object param)
        {
            return Dapper.SqlMapper.ExecuteAsync(connection, sql, param, transaction, commandType: commandType);
        }

        public static Task<IEnumerable<E>> DapperQuery<E>(DbConnection connection, string sql, CommandType commandType, IDbTransaction transaction, object param)
        {
            return Dapper.SqlMapper.QueryAsync<E>(connection, sql, param, transaction, commandType: commandType);
        }

        public static Task<E> DapperQueryFirst<E>(DbConnection connection, string sql, CommandType commandType, IDbTransaction transaction, object param)
        {
            return Dapper.SqlMapper.QueryFirstOrDefaultAsync<E>(connection, sql, param, transaction, commandType: commandType);
        }

        public static Dapper.SqlMapper.ICustomQueryParameter DapperAsTableValuedParameter(DataTable table)
        {
            return Dapper.SqlMapper.AsTableValuedParameter(table);
        }
    }
}
