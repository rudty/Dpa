using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
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
            RepositoryPropertyNameInfo propertyNameInfo = ReflectUtils.GetRepositoryPropertyInfo(typeof(T));

            Func<ID, object> idBinder = IRepositoryQuery<T, ID>.GetDefaultIdQueryParameterBinder();
            Func<T, object> entityBinder = IRepositoryQuery<T, ID>.GetDefaultEntityQueryParameterBinder();

            string storeProcedureTableName = GetStoreProcedureTableName(propertyNameInfo.TableName);

            Select = new QueryAndParameter<ID>($"{storeProcedureTableName}_select_dpa_generated", idBinder);
            Insert = new QueryAndParameter<T>($"{storeProcedureTableName}_insert_dpa_generated", entityBinder);
            Update = new QueryAndParameter<T>($"{storeProcedureTableName}_update_dpa_generated", entityBinder);
            Delete = new QueryAndParameter<ID>($"{storeProcedureTableName}_delete_dpa_generated", idBinder);
        }

        private async Task<Dictionary<string, TableType>> GetTableType(DbConnection connection, string tableName)
        {
            IEnumerable<TableType> tableType = await Dapper.SqlMapper.QueryAsync<TableType>(connection, $"select [name] as [ColumnName], [system_type_name] as [ColumnTypeName] from sys.dm_exec_describe_first_result_set('select top 1 * from {tableName}', null, 0);");
            return tableType.ToDictionary(e => e.ColumnName.ToUpper());
        }

        private static string MakeStoreProcedureParameter(Dictionary<string, TableType> tableType, List<RepositoryColumn> propertyNames)
        {
            StringBuilder entityProcedureParameter = new StringBuilder(200);
            for (int i = 0; i < propertyNames.Count; ++i)
            {
                string propertyName = propertyNames[i].PropertyName.ToUpper();
                if (i > 0)
                {
                    entityProcedureParameter.Append(',');
                }
                entityProcedureParameter
                    .Append('@')
                    .Append(propertyName)
                    .Append(' ')
                    .Append(tableType[propertyName].ColumnTypeName);
            }

            return entityProcedureParameter.ToString();
        } 

        internal async Task EnsureStoreProcedure(DbConnection connection)
        {
            RepositoryPropertyNameInfo propertyNameInfo = ReflectUtils.GetRepositoryPropertyInfo(typeof(T));
            Dictionary<string, TableType> tableType = await GetTableType(connection, propertyNameInfo.TableName);
            string idStoreProcedureParameter;

            if (propertyNameInfo.PrimaryKeyPropertyNames.Count == 1)
            {
                string pk = propertyNameInfo.PrimaryKeyPropertyNames[0].ColumnName.ToUpper();
                string idColumnType = tableType[pk].ColumnTypeName;
                idStoreProcedureParameter = "@id " + idColumnType;
            } 
            else
            {
                idStoreProcedureParameter = MakeStoreProcedureParameter(tableType, propertyNameInfo.PrimaryKeyPropertyNames);
            }

            string entityProcedureParameter = MakeStoreProcedureParameter(tableType, propertyNameInfo.PropertyNames);
            TextRepositoryQuery<T, ID> textRepositoryQuery = new TextRepositoryQuery<T, ID>();

            string storeProcedureTableName = GetStoreProcedureTableName(propertyNameInfo.TableName);

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
            string storeProcedureName = $"{tableName}_{op}_dpa_generated";
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
