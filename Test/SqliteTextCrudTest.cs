using Dpa.Repository;
using System.Threading.Tasks;
using Test.Entity;
using Test.Helper;
using Xunit;

namespace Test
{
    public class SqliteTextCrudTest : SqliteTestClass
    {
        [Fact]
        public async Task Create()
        {
            var repo = await RepositoryGenerator.Default<TestIntKeyEntity, int>(connection);
            Assert.NotNull(repo);

            var repo2 = await RepositoryGenerator.Default<TestMultiKeyEntity, MultiKey>(connection);
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
    }
}
