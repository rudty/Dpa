using Dpa.Repository;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;
using Test.Entity;
using Test.Helper;
using Xunit;

namespace Test
{
    public interface ITxRepository
    {
        const string query = "insert into " + TestIntKeyEntity.TableName + " values(@a, @b);";

        [Query(query)]
        Task my_insertL(DbTransaction tran, int a, string b);

        [Query(query)]
        Task my_insertM(int a, DbTransaction tran, string b);

        [Query(query)]
        Task my_insertR(int a, string b, DbTransaction tran);

        [Query("select * from " + TestIntKeyEntity.TableName)]
        Task<List<TestIntKeyEntity>> SelectAll();
    }

    public class TransactionTest : SqlServerTestClass
    {
        [Fact]
        public async Task InsertAndCommit()
        {
            var repo = await RepositoryGenerator.Custom<ITxRepository>(connection);

            using (var tran = await connection.BeginTransactionAsync())
            {
                await repo.my_insertL(tran, 1, "2");
                await repo.my_insertM(3, tran, "4");
                await repo.my_insertR(5, "6", tran);

                await tran.CommitAsync();

                var r0 = await repo.SelectAll();
                r0.Sort();
                Assert.Equal(new List<TestIntKeyEntity>() {
                    new TestIntKeyEntity(1, "2"),
                    new TestIntKeyEntity(3, "4"),
                    new TestIntKeyEntity(5, "6"),
                }, r0);
            }
        }

        [Fact]
        public async Task InsertAndRollback()
        {
            var repo = await RepositoryGenerator.Custom<ITxRepository>(connection);

            using (var tran = await connection.BeginTransactionAsync())
            {
                await repo.my_insertL(tran, 1, "2");
                await repo.my_insertM(3, tran, "4");
                await repo.my_insertR(5, "6", tran);

                await tran.RollbackAsync();

                var r0 = await repo.SelectAll();
                Assert.Empty(r0);
            }
        }

        [Fact]
        public async Task CrudCommit()
        {
            var repo = await RepositoryGenerator.Default<TestIntKeyEntity, int>(connection);
            var insertId = 8779;
            using (var tran = await connection.BeginTransactionAsync())
            {
                var insertValue = new TestIntKeyEntity(insertId, "x" + insertId);
                await repo.InsertRow(insertValue, tran);

                var r0 = await repo.SelectRow(insertId, tran);
                Assert.Equal(insertValue, r0);

                insertValue.Value = "aaa" + insertId;
                await repo.UpdateRow(insertValue, tran);

                var r1 = await repo.SelectRow(insertId, tran);
                Assert.Equal(insertValue, r1);

                await repo.DeleteRow(insertValue.Id, tran);

                var r2 = await repo.SelectRow(insertId, tran);
                Assert.True(null == r2);
                await tran.CommitAsync();
            }
        }


        [Fact]
        public async Task CrudRollback()
        {
            var repo = await RepositoryGenerator.Default<TestIntKeyEntity, int>(connection);
            var insertId = 8779;

            var insertValue = new TestIntKeyEntity(insertId, "x" + insertId);
            await repo.InsertRow(insertValue);

            var r0 = await repo.SelectRow(insertId);
            Assert.Equal(insertValue, r0);

            insertValue.Value = "aaa" + insertId;
            await repo.UpdateRow(insertValue);

            var r1 = await repo.SelectRow(insertId);
            Assert.Equal(insertValue, r1);
            using (var tran = await connection.BeginTransactionAsync())
            {
                await repo.DeleteRow(insertValue.Id, tran);
                await tran.RollbackAsync();
            }
            var r2 = await repo.SelectRow(insertId);
            Assert.Equal(insertValue, r2);
        }
    }
}
