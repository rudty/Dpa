using System;
using Xunit;
using Dpa.Repository;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using System.Threading.Tasks;
using System.Collections.Generic;
using Test.Entity;


namespace Test
{
    public class MultiplePrimaryKeyTest : IDisposable
    {
        private DbConnection connection;

        public MultiplePrimaryKeyTest()
        {
            connection = new SqliteConnection(@"Data Source=:memory:");
            connection.Open();

            DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = MultiPkEntity.SqliteCreateTableQuery;
            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            cmd.CommandText = $"insert into {MultiPkEntity.TableName} values(1, 'a', 'a'), (2, 'a', 'b'), (3, 'a', 'c'), (1, 'b', 'k');";
            cmd.ExecuteNonQuery();

            cmd.Dispose();
        }

        public void Dispose()
        {
            connection.Dispose();
        }

        [Fact]
        public async Task Foo()
        {
            ICrudRepository<MultiPkEntity, MultiPk> repository = await RepositoryGenerator.Default<MultiPkEntity, MultiPk> (connection);
            var r = await repository.SelectRow(new MultiPk(1, "a"));
            Assert.Equal(r, new MultiPkEntity(1, "a", "a"));
        }
    }
}
