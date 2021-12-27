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
    public class DeleteTest : IDisposable
    {
        private DbConnection connection;

        public DeleteTest()
        {
            connection = new SqliteConnection(@"Data Source=:memory:");
            connection.Open();

            DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = TestIntKeyEntity.SqliteCreateTableQuery;
            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            cmd.CommandText = $"insert into {TestIntKeyEntity.TableName} values(1, 'a'), (2, 'b'), (3, 'c'), (999, 'd'), (999, 'e'), (999, 'f');";
            cmd.ExecuteNonQuery();

            cmd.CommandText = TestStringKeyEntity.SqliteCreateTableQuery;
            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            cmd.CommandText = $"insert into {TestStringKeyEntity.TableName} values('a', 1), ('b', 2), ('c', 3), ('z', 4), ('z',5), ('z',6);";
            cmd.ExecuteNonQuery();

            cmd.Dispose();
        }

        public void Dispose()
        {
            connection.Dispose();
        }

        [Fact]
        public async Task Delete1IntTest()
        {
            ICrudRepository<TestIntKeyEntity, int> repository = await RepositoryGenerator.Default<TestIntKeyEntity, int>(connection);
            int affectedRows = await repository.Delete(1);
            Assert.Equal(1, affectedRows);

            List<TestIntKeyEntity> r = TestIntKeyEntity.SelectEntity(connection, 1);
            Assert.Empty(r);
        }

        [Fact]
        public async Task DeleteManyIntTest()
        {
            ICrudRepository<TestIntKeyEntity, int> repository = await RepositoryGenerator.Default<TestIntKeyEntity, int>(connection);
            int[] deleteRows = new int[] { 1, 2, 3 };
            int affectedRows = await repository.Delete(deleteRows);
            Assert.Equal(3, affectedRows);

            List<TestIntKeyEntity> r = TestIntKeyEntity.SelectEntity(connection, 1, 2, 3);
            Assert.Empty(r);
        }

        [Fact]
        public async Task Delete1StringTest()
        {
            ICrudRepository<TestStringKeyEntity, string> repository = await RepositoryGenerator.Default<TestStringKeyEntity, string>(connection);
            int affectedRows = await repository.Delete("a");
            Assert.Equal(1, affectedRows);

            List<TestStringKeyEntity> r = TestStringKeyEntity.SelectEntity(connection, "a");
            Assert.Empty(r);
        }

        [Fact]
        public async Task UpdateManyStringTest()
        {
            ICrudRepository<TestStringKeyEntity, string> repository = await RepositoryGenerator.Default<TestStringKeyEntity, string>(connection);
            int affectedRows = await repository.Delete(new string[] { "a", "b", "c" });
            Assert.Equal(3, affectedRows);

            List<TestStringKeyEntity> r = TestStringKeyEntity.SelectEntity(connection, "a", "b", "c");
            Assert.Empty(r);
        }

    }
}
