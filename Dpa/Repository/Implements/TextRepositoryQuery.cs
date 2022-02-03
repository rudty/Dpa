using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text;
using System.Reflection;
using Dpa.Repository.Implements.Types;

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
            Entity<PropertyInfo> properties = typeof(T).GetMappingProperties();
            
            string columns = string.Join(',', properties.Select(p => p.ColumnName));
            string cond = GetCond(properties.GetPrimaryKeys().ToList());
            string tableName = ReflectUtils.GetTableName(typeof(T));

            Func<ID, object> idBinder = IRepositoryQuery<T, ID>.GetDefaultIdQueryParameterBinder();
            Func<T, object> entityBinder = IRepositoryQuery<T, ID>.GetDefaultEntityQueryParameterBinder();

            Select = new QueryAndParameter<ID>(GetSelectQuery(columns, tableName, cond), idBinder);
            Insert = new QueryAndParameter<T>(GetInsertQuery(columns, tableName, properties), entityBinder);
            Update = new QueryAndParameter<T>(GetUpdateQuery(tableName, properties, cond), entityBinder);
            Delete = new QueryAndParameter<ID>(GetDeleteQuery(tableName, cond), idBinder);
        }

        private static string GetCond(List<Column<PropertyInfo>> primaryKeyProperies)
        {
            StringBuilder builder = new StringBuilder(100);
            builder.Append("where ");
            for (int i = 0; i < primaryKeyProperies.Count; ++i)
            {
                if (i > 0)
                {
                    builder.Append(" and ");
                }
                builder.Append(primaryKeyProperies[i].ColumnName);
                builder.Append("=@");
                builder.Append(primaryKeyProperies[i].MemberName);
            }

            return builder.ToString();
        }

        private static string GetDeleteQuery(string tableName, string cond)
        {
            return $"delete from {tableName} {cond};";
        }

        private static string GetUpdateQuery(string tableName, Entity<PropertyInfo> props, string cond)
        {
            string updateNames = string.Join(',', props.GetNotPkColumns().Select(p => $"{p.ColumnName}=@{p.MemberName}"));
            return $"update {tableName} set {updateNames} {cond};";
        }

        private static string GetInsertQuery(string columns, string tableName, Entity<PropertyInfo> props)
        {
            string parameters = "@" + string.Join(",@", props.Select(p => p.MemberName));
            return $"insert into {tableName}({columns}) values({parameters});";
        }

        private static string GetSelectQuery(string columns, string tableName, string cond)
        {
            return $"select {columns} from {tableName} {cond};";
        } 
    }
}
