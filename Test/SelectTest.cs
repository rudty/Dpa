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
    public class SelectTest : IDisposable
    {
        private DbConnection connection;

        public SelectTest()
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
        public async Task SelectFirstIntTest()
        {
            ICrudRepository<TestIntKeyEntity, int> repository = await RepositoryGenerator.Default<TestIntKeyEntity, int>(connection);
            TestIntKeyEntity r1 = await repository.SelectFirst(1);
            Assert.Equal(r1, new TestIntKeyEntity(1, "a"));

            TestIntKeyEntity r2 = await repository.SelectFirst(2);
            Assert.Equal(r2, new TestIntKeyEntity(2, "b"));

            TestIntKeyEntity r3 = await repository.SelectFirst(3);
            Assert.Equal(r3, new TestIntKeyEntity(3, "c"));
        }

        [Fact]
        public async Task SelectFirstStringTest()
        {
            ICrudRepository<TestStringKeyEntity, string> repository = await RepositoryGenerator.Default<TestStringKeyEntity, string>(connection);
            TestStringKeyEntity r1 = await repository.SelectFirst("a");
            Assert.Equal(r1, new TestStringKeyEntity("a", 1));

            TestStringKeyEntity r2 = await repository.SelectFirst("b");
            Assert.Equal(r2, new TestStringKeyEntity("b", 2));

            TestStringKeyEntity r3 = await repository.SelectFirst("c");
            Assert.Equal(r3, new TestStringKeyEntity("c", 3));
        }

        [Fact]
        public async Task SelectAllIntTest()
        {
            ICrudRepository<TestIntKeyEntity, int> repository = await RepositoryGenerator.Default<TestIntKeyEntity, int>(connection);
            IReadOnlyCollection<TestIntKeyEntity> r1 = await repository.Select(999);

            TestIntKeyEntity[] c = new TestIntKeyEntity[] {
                new TestIntKeyEntity(999, "d"),
                new TestIntKeyEntity(999, "e"),
                new TestIntKeyEntity(999, "f"),
            };
            
            Assert.Equal(r1, c);
        }

        [Fact]
        public async Task SelectAllStringTest()
        {
            ICrudRepository<TestStringKeyEntity, string> repository = await RepositoryGenerator.Default<TestStringKeyEntity, string>(connection);
            IReadOnlyCollection<TestStringKeyEntity> r1 = await repository.Select("z");

            TestStringKeyEntity[] c = new TestStringKeyEntity[] {
                new TestStringKeyEntity("z", 4),
                new TestStringKeyEntity("z", 5),
                new TestStringKeyEntity("z", 6),
            };

            Assert.Equal(r1, c);
        }
    }
}
