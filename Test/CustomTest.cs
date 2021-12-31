using System;
using Xunit;
using Dpa.Repository;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using System.Threading.Tasks;
using Test.Entity;
namespace Test
{
    public class CustomTest : IDisposable
    {
        private DbConnection connection;

        public CustomTest()
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

        public interface IHello
        {
            [Query("select * from " + TestIntKeyEntity.TableName + " where id = @id")]
            Task<TestIntKeyEntity> GetHello(int id);
        }

        [Fact]
        public async Task Create()
        {
            var repo = await RepositoryGenerator.Custom<IHello>(connection);
            Assert.NotNull(repo);
        }
    }
}
