using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text;

namespace Dpa.Repository.Implements
{
    public class TextRepositoryQuery<T, ID> : IRepositoryQuery<T, ID>
    {
        public QueryAndParameter<ID> Select { get; }

        public QueryAndParameter<T> Insert { get; }

        public QueryAndParameter<T> Update { get; }

        public QueryAndParameter<ID> Delete { get; }

        CommandType IRepositoryQuery<T, ID>.CommandType => CommandType.Text;

        public TextRepositoryQuery()
        {
            RepositoryPropertyNameInfo propertyNameInfo = ReflectUtils.GetRepositoryPropertyInfo(typeof(T));
            string columns = string.Join(',', propertyNameInfo.PropertyNames);
            string cond = GetCond(propertyNameInfo.PrimaryKeyPropertyNames);

            Func<ID, object> idBinder = IRepositoryQuery<T, ID>.GetDefaultIdQueryParameterBinder();
            Func<T, object> entityBinder = IRepositoryQuery<T, ID>.GetDefaultEntityQueryParameterBinder();

            Select = new QueryAndParameter<ID>(GetSelectQuery(columns, propertyNameInfo.TableName, cond), idBinder);
            Insert = new QueryAndParameter<T>(GetInsertQuery(columns, propertyNameInfo.TableName, propertyNameInfo.PropertyNames), entityBinder);
            Update = new QueryAndParameter<T>(GetUpdateQuery(propertyNameInfo.TableName, propertyNameInfo.PropertyNames, propertyNameInfo.PrimaryKeyPropertyNames), entityBinder);
            Delete = new QueryAndParameter<ID>(GetDeleteQuery(propertyNameInfo.TableName, cond), idBinder);
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
    }
}
