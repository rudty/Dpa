using Dpa.Repository.Implements.Types;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dpa.Repository.Implements
{
    public class StoreProcedureRepositoryQuery<T, ID> : IRepositoryQuery<T, ID>
    {
        public QueryAndParameter<ID> Select { get; }

        public QueryAndParameter<T> Insert { get; }

        public QueryAndParameter<T> Update { get; }

        public QueryAndParameter<ID> Delete { get; }

        CommandType IRepositoryQuery<T, ID>.CommandType => CommandType.StoredProcedure;

        public StoreProcedureRepositoryQuery()
        {
            Entity<PropertyInfo> props = typeof(T).GetMappingProperties();
            string tableName = ReflectUtils.GetTableName(typeof(T));

            Func<ID, object> idBinder = IRepositoryQuery<T, ID>.GetDefaultIdQueryParameterBinder();
            Func<T, object> entityBinder = IRepositoryQuery<T, ID>.GetDefaultEntityQueryParameterBinder();

            string storeProcedureTableName = GetStoreProcedureTableName(tableName);

            Select = new QueryAndParameter<ID>(GetStoreProcedureName(storeProcedureTableName, "select"), idBinder);
            Insert = new QueryAndParameter<T>(GetStoreProcedureName(storeProcedureTableName, "insert"), entityBinder);
            Update = new QueryAndParameter<T>(GetStoreProcedureName(storeProcedureTableName, "update"), entityBinder);
            Delete = new QueryAndParameter<ID>(GetStoreProcedureName(storeProcedureTableName, "delete"), idBinder);
        }

        private async Task<Dictionary<string, TableType>> GetTableType(DbConnection connection, string tableName)
        {
            IEnumerable<TableType> tableType = await Dapper.SqlMapper.QueryAsync<TableType>(connection, $"select [name] as [ColumnName], [system_type_name] as [ColumnTypeName] from sys.dm_exec_describe_first_result_set('select top 1 * from {tableName}', null, 0);");
            return tableType.ToDictionary(e => e.ColumnName.ToUpper());
        }
        
        private static string GetStoreProcedureName(string tableName, string op)
        {
            return $"{tableName}_{op}_dpa_generated";
        }

        private static string MakeStoreProcedureParameter(Dictionary<string, TableType> tableType, List<Column<PropertyInfo>> propertyNames)
        {
            StringBuilder entityProcedureParameter = new StringBuilder(200);
            for (int i = 0; i < propertyNames.Count; ++i)
            {
                string propertyName = propertyNames[i].MemberName.ToUpper();
                string columnName = propertyNames[i].ColumnName.ToUpper();
                if (i > 0)
                {
                    entityProcedureParameter.Append(',');
                }
                entityProcedureParameter
                    .Append('@')
                    .Append(propertyName)
                    .Append(' ')
                    .Append(tableType[columnName].ColumnTypeName);
            }

            return entityProcedureParameter.ToString();
        } 

        internal async Task EnsureStoreProcedure(DbConnection connection)
        {
            Entity<PropertyInfo> props = typeof(T).GetMappingProperties();
            string tableName = ReflectUtils.GetTableName(typeof(T));
            Dictionary<string, TableType> tableType = await GetTableType(connection, tableName);
            string idStoreProcedureParameter;
            List<Column<PropertyInfo>> pkProps = props.GetPrimaryKeys().ToList();

            if (pkProps.Count == 1)
            {
                string pk = pkProps[0].ColumnName.ToUpper();
                string idColumnType = tableType[pk].ColumnTypeName;
                idStoreProcedureParameter = "@id " + idColumnType;
            } 
            else
            {
                idStoreProcedureParameter = MakeStoreProcedureParameter(tableType, pkProps);
            }

            string entityProcedureParameter = MakeStoreProcedureParameter(tableType, props);
            TextRepositoryQuery<T, ID> textRepositoryQuery = new TextRepositoryQuery<T, ID>();

            string storeProcedureTableName = GetStoreProcedureTableName(tableName);

            await DropAndCreateProcedure(connection, storeProcedureTableName, idStoreProcedureParameter, "select", textRepositoryQuery.Select.query);
            await DropAndCreateProcedure(connection, storeProcedureTableName, entityProcedureParameter, "insert", textRepositoryQuery.Insert.query);
            await DropAndCreateProcedure(connection, storeProcedureTableName, entityProcedureParameter, "update", textRepositoryQuery.Update.query);
            await DropAndCreateProcedure(connection, storeProcedureTableName, idStoreProcedureParameter, "delete", textRepositoryQuery.Delete.query);
 
            return;
        }

        private static string GetStoreProcedureTableName(string tableName)
        {
            Regex regex = new Regex("[\\@\\[\\]]");
            return regex.Replace(tableName, string.Empty);
        }

        private static async Task DropAndCreateProcedure(DbConnection connection, string tableName, string storeProcedureParameter, string op, string query)
        {
            string storeProcedureName = GetStoreProcedureName(tableName, op);
            using (DbCommand command = connection.CreateCommand())
            {
                command.CommandText = $"drop proc if exists {storeProcedureName};";
                await command.ExecuteNonQueryAsync();

                command.CommandText = $@"
create proc {storeProcedureName}
{storeProcedureParameter}
as
begin
    set nocount on;
    {query}
end";
                await command.ExecuteNonQueryAsync();
            }
        }

        private struct TableType
        {
            public string ColumnTypeName { get; set; }

            public string ColumnName { get; set; }
        }

    }
}
