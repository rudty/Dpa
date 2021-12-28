using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using System.Data.SqlClient;
using System.Data.Common;
using Test.Entity;
using Dpa.Repository;
using System.Threading.Tasks;

namespace Test
{
    public class StoreProcedureTest : IDisposable
    {
        private DbConnection connection;

        public StoreProcedureTest()
        {
            connection = new SqlConnection("server=localhost;Integrated Security=SSPI; database=test");
            connection.Open();

            DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = "drop table if exists " + TestIntKeyEntity.TableName;
            cmd.ExecuteNonQuery();

            cmd.CommandText = TestIntKeyEntity.SqliteCreateTableQuery;
            cmd.ExecuteNonQuery();

            cmd = connection.CreateCommand();
            cmd.CommandText = $"insert into {TestIntKeyEntity.TableName} values(1, 'a'), (2, 'b'), (3, 'c'), (999, 'd'), (999, 'e'), (999, 'f');";
            cmd.ExecuteNonQuery();

            cmd.Dispose();
        }

        public void Dispose()
        {
            connection.Dispose();
        }

        [Fact]
        public async Task SelectStoreProcedure()
        {
            IStoreProcedureCrudRepository<TestIntKeyEntity, int> repository = await RepositoryGenerator.SqlServerStoreProcedure<TestIntKeyEntity, int>(connection);
            await repository.EnsureStoreProcedure();

            var r = await repository.SelectFirst(1);
            Assert.Equal(r, new TestIntKeyEntity(1, "a"));
        }

        [Fact]
        public async Task UpdateStoreProcedure()
        {
            IStoreProcedureCrudRepository<TestIntKeyEntity, int> repository = await RepositoryGenerator.SqlServerStoreProcedure<TestIntKeyEntity, int>(connection);
            await repository.EnsureStoreProcedure();

            TestIntKeyEntity[] updateRows = new TestIntKeyEntity[] {
                new TestIntKeyEntity(1, "q"),
                new TestIntKeyEntity(2, "w"),
                new TestIntKeyEntity(3, "e"),
            };

            // nocount on 일때는 affectedRows 를 반환하지 않음 
            await repository.Update(updateRows);

            List<TestIntKeyEntity> r = TestIntKeyEntity.SelectEntity(connection, 1, 2, 3);
            Assert.Equal(r, updateRows);
        }

        [Fact]
        public async Task DeleteManyIntTest()
        {
            IStoreProcedureCrudRepository<TestIntKeyEntity, int> repository = await RepositoryGenerator.SqlServerStoreProcedure<TestIntKeyEntity, int>(connection);
            int[] deleteRows = new int[] { 1, 2, 3 };
            await repository.Delete(deleteRows);

            List<TestIntKeyEntity> r = TestIntKeyEntity.SelectEntity(connection, 1, 2, 3);
            Assert.Empty(r);
        }

        [Fact]
        public async Task InsertManyIntTest()
        {
            IStoreProcedureCrudRepository<TestIntKeyEntity, int> repository = await RepositoryGenerator.SqlServerStoreProcedure<TestIntKeyEntity, int>(connection);
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
    }
}
