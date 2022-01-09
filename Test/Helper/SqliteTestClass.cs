using Microsoft.Data.Sqlite;
using System;
using System.Data.Common;
using System.Threading.Tasks;
using Test.Entity;
using Xunit;

namespace Test.Helper
{
    public class SqliteTestClass : IAsyncLifetime
    {
        protected DbConnection connection;

        protected SqliteTestClass()
        {
            connection = new SqliteConnection(@"Data Source=:memory:");
            connection.Open();
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
