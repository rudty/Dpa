using System;
using Xunit;
using Dpa.Repository;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using System.Threading.Tasks;
using System.Data;
using System.Collections.Generic;
using Test.Entity;

namespace Test
{
    public class GeneratorTest : IDisposable
    {
        private DbConnection connection;

        public GeneratorTest()
        {
            connection = new SqliteConnection(@"Data Source=:memory:");
            connection.Open();

            DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = TestIntKeyEntity.SqliteCreateTableQuery;
            cmd.ExecuteNonQuery();

            cmd.CommandText = TestStringKeyEntity.SqliteCreateTableQuery;
            cmd.ExecuteNonQuery();

            cmd.Dispose();
        }

        public void Dispose()
        {
            connection.Dispose();
        }

        [Fact]
        public async Task Insert1IntTest()
        {
            ICrudRepository<TestIntKeyEntity, int> repository = await RepositoryGenerator.Default<TestIntKeyEntity, int>(connection);
            TestIntKeyEntity i1 = new TestIntKeyEntity(8888, "x");
            await repository.Insert(i1);

            TestIntKeyEntity readEntity = TestIntKeyEntity.SelectEntityFirst(connection, 8888);
            Assert.Equal(i1, readEntity);
        }

        [Fact]
        public async Task InsertManyIntTest()
        {
            ICrudRepository<TestIntKeyEntity, int> repository = await RepositoryGenerator.Default<TestIntKeyEntity, int>(connection);
            TestIntKeyEntity[] values = new TestIntKeyEntity[] {
                new TestIntKeyEntity(8891, "x1"),
                new TestIntKeyEntity(8891, "x2"),
                new TestIntKeyEntity(8891, "x3"),
                new TestIntKeyEntity(8891, "x4"),
            };

            await repository.Insert(values);

            List<TestIntKeyEntity> readEntity = TestIntKeyEntity.SelectEntity(connection, 8891);
            Assert.Equal(values, readEntity);
        }

        [Fact]
        public async Task Insert1StringTest()
        {
            ICrudRepository<TestStringKeyEntity, int> repository = await RepositoryGenerator.Default<TestStringKeyEntity, int>(connection);
            TestStringKeyEntity i1 = new TestStringKeyEntity("a", 1);
            await repository.Insert(i1);

            TestStringKeyEntity readEntity = TestStringKeyEntity.SelectEntityFirst(connection, "a");
            Assert.Equal(i1, readEntity);
        }

        [Fact]
        public async Task InsertManyStringTest()
        {
            ICrudRepository<TestStringKeyEntity, int> repository = await RepositoryGenerator.Default<TestStringKeyEntity, int>(connection);
            TestStringKeyEntity[] values = new TestStringKeyEntity[] {
                new TestStringKeyEntity("z", 8886),
                new TestStringKeyEntity("z", 8887),
                new TestStringKeyEntity("z", 8898),
                new TestStringKeyEntity("z", 8889),
            };

            await repository.Insert(values);

            List<TestStringKeyEntity> readEntity = TestStringKeyEntity.SelectEntity(connection, "z");
            Assert.Equal(values, readEntity);
        }
    }
}
