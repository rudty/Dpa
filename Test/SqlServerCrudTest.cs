using Dpa.Repository;
using System.Threading.Tasks;
using Test.Entity;
using Test.Helper;
using Xunit;

namespace Test
{
    public class SqlServerCrudTest : SqlServerTestClass
    {
        [Fact]
        public async Task Create()
        {
            var repo = await RepositoryGenerator.SqlServerStoreProcedure<TestIntKeyEntity, int>(connection);
            Assert.NotNull(repo);

            var repo2 = await RepositoryGenerator.Default<TestIntKeyEntity, int>(connection);
            Assert.NotNull(repo2);
        }

        [Fact]
        public async Task Default()
        {
            var repo = await RepositoryGenerator.Default<TestIntKeyEntity, int>(connection);
            await CrudTestHelper.TestAll(repo);
        }

        [Fact]
        public async Task DefaultMultiKey()
        {
            var repo = await RepositoryGenerator.Default<TestMultiKeyEntity, MultiKey>(connection);
            await CrudTestHelper.TestAll(repo);
        }

        [Fact]
        public async Task StoreProcedure()
        {
            var repo = await RepositoryGenerator.SqlServerStoreProcedure<TestIntKeyEntity, int>(connection);
            await repo.EnsureStoreProcedure();
            await CrudTestHelper.TestAll(repo);
        }

        [Fact]
        public async Task StoreProcedureMultikey()
        {
            var repo = await RepositoryGenerator.SqlServerStoreProcedure<TestMultiKeyEntity, MultiKey>(connection);
            await repo.EnsureStoreProcedure();
            await CrudTestHelper.TestAll(repo);
        }

        [Fact]
        public async Task Foo()
        {
            System.Data.SqlClient.SqlConnection conn = (System.Data.SqlClient.SqlConnection)connection;
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"create proc #hello
@a varchar(600)
as
select* from sys.all_columns where name like @a";
            cmd.ExecuteNonQuery();

      
            Dapper.DynamicParameters p = new Dapper.DynamicParameters();
            var r = Dapper.SqlMapper.Query(connection, "exec #hello @a;", new
            {
                a = "c",
                b = 3,
                c = 7
            });

            System.Console.WriteLine(r);
        }
    }
}
