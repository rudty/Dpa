using System;
using Xunit;
using Dpa.Repository;
using System.Threading.Tasks;
using Test.Entity;
using Test.Helper;
namespace Test
{
    public class CustomCrudTest : SqlServerTestClass
    {
        public interface ICustomAndCrud : ICrudRepository<TestIntKeyEntity, int>
        {
        }

        public interface ICustomAndSp : IStoreProcedureCrudRepository<TestIntKeyEntity, int>
        {
        }

        [Fact]
        public async Task Create()
        {
            var repo = await RepositoryGenerator.Custom<ICustomAndCrud>(connection);
            Assert.NotNull(repo);
            var repo2 = await RepositoryGenerator.Custom<ICustomAndSp>(connection);
            Assert.NotNull(repo2);
        }

        [Fact]
        public async Task CustomCrud()
        {
            var repo = await RepositoryGenerator.Custom<ICustomAndCrud>(connection);
            await CrudTestHelper.TestAll(repo);
        }

        [Fact]
        public async Task CustomSp()
        {
            var repo = await RepositoryGenerator.Custom<ICustomAndSp>(connection);
            await repo.EnsureStoreProcedure();
            await CrudTestHelper.TestAll(repo);
        }
    }
}
