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
    [Collection("sqlserver")]
    public class StoreProcedureTest : IClassFixture<SqlServerDatabaseFixture>
    {
        private DbConnection connection;

        public StoreProcedureTest(SqlServerDatabaseFixture fixture)
        {
            this.connection = fixture.SqlServerConnection;
        }

        [Fact]
        public async Task SelectStoreProcedure()
        {
            IStoreProcedureCrudRepository<TestIntKeyEntity, int> repository = await RepositoryGenerator.SqlServerStoreProcedure<TestIntKeyEntity, int>(connection);
            await repository.EnsureStoreProcedure();

            var r = await repository.SelectRow(1);
            Assert.Equal(r, new TestIntKeyEntity(1, "a"));
        }

        [Fact]
        public async Task UpdateStoreProcedure()
        {
            IStoreProcedureCrudRepository<TestIntKeyEntity, int> repository = await RepositoryGenerator.SqlServerStoreProcedure<TestIntKeyEntity, int>(connection);
            await repository.EnsureStoreProcedure();

            TestIntKeyEntity[] updateRows = new TestIntKeyEntity[] {
                new TestIntKeyEntity(10, "q"),
                new TestIntKeyEntity(11, "w"),
                new TestIntKeyEntity(12, "e"),
            };

            // nocount on 일때는 affectedRows 를 반환하지 않음 
            await repository.Update(updateRows);

            List<TestIntKeyEntity> r = TestIntKeyEntity.SelectEntity(connection, 10, 11, 12);
            Assert.Equal(r, updateRows);
        }

        [Fact]
        public async Task DeleteManyIntTest()
        {
            IStoreProcedureCrudRepository<TestIntKeyEntity, int> repository = await RepositoryGenerator.SqlServerStoreProcedure<TestIntKeyEntity, int>(connection);
            int[] deleteRows = new int[] { 7, 8, 9 };
            await repository.Delete(deleteRows);

            List<TestIntKeyEntity> r = TestIntKeyEntity.SelectEntity(connection, 7, 8, 9);
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
