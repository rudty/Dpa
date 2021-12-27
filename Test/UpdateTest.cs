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
    public class UpdateTest : IDisposable
    {
        private DbConnection connection;

        public UpdateTest()
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
        public async Task Update1IntTest()
        {
            ICrudRepository<TestIntKeyEntity, int> repository = await RepositoryGenerator.Default<TestIntKeyEntity, int>(connection);
            TestIntKeyEntity updateRow = new TestIntKeyEntity(1, "q");
            int affectedRows = await repository.Update(updateRow);
            Assert.Equal(1, affectedRows);

            TestIntKeyEntity r = TestIntKeyEntity.SelectEntityFirst(connection, 1);
            Assert.Equal(r, updateRow);
        }

        [Fact]
        public async Task UpdateManyIntTest()
        {
            ICrudRepository<TestIntKeyEntity, int> repository = await RepositoryGenerator.Default<TestIntKeyEntity, int>(connection);
            TestIntKeyEntity[] updateRows = new TestIntKeyEntity[] {
                new TestIntKeyEntity(1, "q"),
                new TestIntKeyEntity(2, "w"),
                new TestIntKeyEntity(3, "e"),
            };
            int affectedRows = await repository.Update(updateRows);
            Assert.Equal(3, affectedRows);

            List<TestIntKeyEntity> r = TestIntKeyEntity.SelectEntity(connection, 1, 2, 3);
            Assert.Equal(r, updateRows);
        }

        [Fact]
        public async Task Update1StringTest()
        {
            ICrudRepository<TestStringKeyEntity, string> repository = await RepositoryGenerator.Default<TestStringKeyEntity, string>(connection);
            TestStringKeyEntity updateRow = new TestStringKeyEntity("a", 9999);
            int affectedRows = await repository.Update(updateRow);
            Assert.Equal(1, affectedRows);

            TestStringKeyEntity r = TestStringKeyEntity.SelectEntityFirst(connection, "a");
            Assert.Equal(r, updateRow);
        }

        [Fact]
        public async Task UpdateManyStringTest()
        {
            ICrudRepository<TestStringKeyEntity, string> repository = await RepositoryGenerator.Default<TestStringKeyEntity, string>(connection);
            TestStringKeyEntity[] updateRows = new TestStringKeyEntity[] {
                new TestStringKeyEntity("a", 9998),
                new TestStringKeyEntity("b", 9997),
                new TestStringKeyEntity("c", 9996),
            };
            int affectedRows = await repository.Update(updateRows);
            Assert.Equal(3, affectedRows);

            List<TestStringKeyEntity> r = TestStringKeyEntity.SelectEntity(connection, "a", "b", "c");
            Assert.Equal(r, updateRows);
        }


    }
}
