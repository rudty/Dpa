using Dpa.Repository;
using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Test.Entity;
using Xunit;

namespace Test.Helper
{
    public class SqlServerTestClass : IAsyncLifetime
    {
        protected DbConnection connection { get; }

        protected SqlServerTestClass()
        {
            connection = new SqlConnection("server=localhost;Integrated Security=SSPI; database=tempdb; MultipleActiveResultSets=true;");
            connection.Open();

            Dapper.SqlMapper.AddTypeMap(typeof(string), System.Data.DbType.AnsiString);
        }

        async Task IAsyncLifetime.InitializeAsync()
        {
            using (DbCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = TestIntKeyEntity.CreateTableQuery;
                await cmd.ExecuteNonQueryAsync();

                cmd.CommandText = TestMultiKeyEntity.CreateTableQuery;
                await cmd.ExecuteNonQueryAsync();
            }
        }

        Task IAsyncLifetime.DisposeAsync()
        {
            return connection.CloseAsync();
        }
    }
}
