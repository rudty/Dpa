using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using Test.Entity;
using Xunit;

namespace Test
{
    [CollectionDefinition("sqlserver")]
    public class SqlServerCollection : ICollectionFixture<SqlServerDatabaseFixture>
    {
    }

    public class SqlServerDatabaseFixture : IDisposable
    {
        public DbConnection SqlServerConnection { get; }

        public SqlServerDatabaseFixture()
        {
            SqlServerConnection = new SqlConnection("server=localhost;Integrated Security=SSPI; database=test; MultipleActiveResultSets=true;");
            SqlServerConnection.Open();

            DbCommand cmd = SqlServerConnection.CreateCommand();
            cmd.CommandText = "drop table if exists " + TestIntKeyEntity.TableName;
            cmd.ExecuteNonQuery();

            cmd.CommandText = TestIntKeyEntity.SqliteCreateTableQuery;
            cmd.ExecuteNonQuery();

            cmd.CommandText = $"insert into {TestIntKeyEntity.TableName} values(1, 'a'), (2, 'b'), (3, 'c'), (999, 'd'), (999, 'e'), (999, 'f'),(7, 'g'), (8, 'h'), (9, 'i'),(10, 'j'), (11, 'k'), (12, 'l'), (13, 'M');";
            cmd.ExecuteNonQuery();
        }

        public void Dispose()
        {
            SqlServerConnection.Dispose();
        }
    }
}
