using Microsoft.Data.Sqlite;
using System;
using System.Data.Common;
using Test.Entity;

namespace Test.Helper
{
    public class SqliteTestClass : IDisposable
    {
        protected DbConnection connection;

        protected SqliteTestClass()
        {
            connection = new SqliteConnection(@"Data Source=:memory:");
            connection.Open();

            DbCommand cmd = connection.CreateCommand();

            cmd.CommandText = TestIntKeyEntity.CreateTableQuery;
            cmd.ExecuteNonQuery();

            cmd.CommandText = TestMultiKeyEntity.CreateTableQuery;
            cmd.ExecuteNonQuery();
        }

        public void Dispose()
        {
            connection.Dispose();
        }
    }
}
