using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Dpa.Repository.Implements
{
    internal class TextRepositoryQuery<T, ID> : IRepositoryQuery<T, ID>
    {
        public QueryAndParameter<ID> Select { get; }

        public QueryAndParameter<T> Insert { get; }

        public QueryAndParameter<T> Update { get; }

        public QueryAndParameter<ID> Delete { get; }

        public TextRepositoryQuery()
        {
            RepositoryPropertyNameInfo propertyNameInfo = ReflectUtils.GetRepositoryPropertyInfo(typeof(T));
            string tableName = ReflectUtils.GetTableName(typeof(T));
            string columns = string.Join(',', propertyNameInfo.PropertyNames);
            string cond = GetCond(propertyNameInfo.PrimaryKeyPropertyNames);

            Func<ID, object> idParamBinder;
            Type idType = typeof(ID);
            if (idType.IsPrimitive || idType == typeof(string))
            {
                idParamBinder = DefaultIdQueryParameterBinder;
            }
            else
            {
                idParamBinder = DefaultEntityQueryParameterBinder;
            }

            Select = new QueryAndParameter<ID>(GetSelectQuery(columns, tableName, cond), idParamBinder);
            Insert = new QueryAndParameter<T>(GetInsertQuery(columns, tableName, propertyNameInfo.PropertyNames), DefaultEntityQueryParameterBinder);
            Update = new QueryAndParameter<T>(GetUpdateQuery(tableName, propertyNameInfo.PropertyNames, propertyNameInfo.PrimaryKeyPropertyNames), DefaultEntityQueryParameterBinder);
            Delete = new QueryAndParameter<ID>(GetDeleteQuery(tableName, cond), idParamBinder);
        }

        private static string GetCond(List<string> primaryKeyPropertyName)
        {
            if (primaryKeyPropertyName.Count == 1)
            {
                return $"where {primaryKeyPropertyName[0]} = @id";
            }

            StringBuilder builder = new StringBuilder(100);
            builder.Append("where ");
            for (int i = 0; i < primaryKeyPropertyName.Count; ++i)
            {
                if (i > 0)
                {
                    builder.Append(" and ");
                }
                builder.Append(primaryKeyPropertyName[i]);
                builder.Append("=@");
                builder.Append(primaryKeyPropertyName[i]);
            }

            return builder.ToString();
        }

        private static string GetDeleteQuery(string tableName, string cond)
        {
            return $"delete from {tableName} {cond};";
        }
        private static string GetUpdateQuery(string tableName, List<string> propertyNames, List<string> pkNames)
        {
            string updateNames = string.Join(',', propertyNames.Select(n => $"{n}=@{n}"));
            string whereNames = string.Join(" and ", pkNames.Select(n => $"{n}=@{n}"));
            return $"update {tableName} set {updateNames} where {whereNames};";
        }

        private static string GetInsertQuery(string columns, string tableName, List<string> propertyNames)
        {
            string parameters = "@" + string.Join(",@", propertyNames);
            return $"insert into {tableName}({columns}) values({parameters});";
        }

        private static string GetSelectQuery(string columns, string tableName, string cond)
        {
            return $"select {columns} from {tableName} {cond};";
        }

        private static object DefaultIdQueryParameterBinder<E>(E id)
        {
            return new { id };
        }

        private static object DefaultEntityQueryParameterBinder<E>(E value)
        {
            return value;
        }
    }
}
