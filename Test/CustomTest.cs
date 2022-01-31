using System;
using Xunit;
using Dpa.Repository;
using System.Threading.Tasks;
using Test.Entity;
using System.Collections.Generic;
using System.Data;
using Test.Helper;

namespace Test
{
    public class MyDateTime
    {
        public DateTime NowDate { get; set; }
    }

    public interface ISpDatabase
    {
        public class SpDatabaseResult
        {
            public string DATABASE_NAME { get; set; }
            public int DATABASE_SIZE { get; set; }
            public int REMARKS { get; set; }
        }

        Task<IEnumerable<SpDatabaseResult>> sp_databases();
    }

    public interface ISpDatabase_value_tuple
    {
        Task<IEnumerable<(string DATABASE_NAME, int DATABASE_SIZE, int REMARKS)>> sp_databases();

        [Query("sp_databases", CommandType.StoredProcedure)]
        Task<(string DATABASE_NAME, int DATABASE_SIZE, int REMARKS)> sp_databases_first();
    }

    public interface IGetDate
    {
        [Query("#CustomTestDateProcedure", CommandType.StoredProcedure)]
        Task<MyDateTime> CustomTestDateProcedure();
    }

    public interface IIntRepository
    {
        [Query("insert into " + TestIntKeyEntity.TableName + " values(@Id, @Value);")]
        Task AddEntity(TestIntKeyEntity entity);

        [Query("insert into " + TestIntKeyEntity.TableName + " values(@Id, '2');")]
        Task AddKeyAnd2(TestIntKeyEntity entity);

        [Query("select * from " + TestIntKeyEntity.TableName + " where id = @id;")]
        Task<TestIntKeyEntity> GetEntity(int id);

        [Query("#select_me", CommandType.StoredProcedure)]
        Task<TestIntKeyEntity> SelectMe(TestIntKeyEntity entity);
    }

    public class CustomTest : SqlServerTestClass
    {
        [Fact]
        public async Task Create()
        {
            var repo = await RepositoryGenerator.Custom<IIntRepository>(connection);
            Assert.NotNull(repo);
        }

        [Fact]
        public async Task SelectHello1()
        {
            var repo = await RepositoryGenerator.Custom<IIntRepository>(connection);
            await repo.AddEntity(new TestIntKeyEntity(1, "a"));
            await repo.AddEntity(new TestIntKeyEntity(2, "b"));
            await repo.AddEntity(new TestIntKeyEntity(3, "c"));

            var entity = await repo.GetEntity(1);
            Assert.Equal(new TestIntKeyEntity(1, "a"), entity);

            entity = await repo.GetEntity(2);
            Assert.Equal(new TestIntKeyEntity(2, "b"), entity);

            entity = await repo.GetEntity(3);
            Assert.Equal(new TestIntKeyEntity(3, "c"), entity);
        }

        [Fact]
        public async Task SelectHello2()
        {
            var repo = await RepositoryGenerator.Custom<IIntRepository>(connection);
            await repo.AddKeyAnd2(new TestIntKeyEntity(1, "a"));
            await repo.AddKeyAnd2(new TestIntKeyEntity(2, "b"));
            await repo.AddKeyAnd2(new TestIntKeyEntity(3, "c"));

            var entity = await repo.GetEntity(1);
            Assert.Equal(new TestIntKeyEntity(1, "2"), entity);

            entity = await repo.GetEntity(2);
            Assert.Equal(new TestIntKeyEntity(2, "2"), entity);

            entity = await repo.GetEntity(3);
            Assert.Equal(new TestIntKeyEntity(3, "2"), entity);
        }

        [Fact]
        public async Task GetDateProcedure()
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
create proc #CustomTestDateProcedure
as
begin
select getdate() as [NowDate]
end";
            await cmd.ExecuteNonQueryAsync();

            cmd.Dispose();

            var repo = await RepositoryGenerator.Custom<IGetDate>(connection);
            var r = await repo.CustomTestDateProcedure();
            Assert.True(r.NowDate.Ticks > 0);
        }

        [Fact]
        public async Task SpDatabases_struct()
        {
            var repo = await RepositoryGenerator.Custom<ISpDatabase>(connection);
            var r = await repo.sp_databases();
            Assert.NotEmpty(r);
            foreach (var db in r)
            {
                Assert.NotEmpty(db.DATABASE_NAME);
                Assert.True(db.DATABASE_SIZE > 0);
            }
        }

        [Fact]
        public async Task SpDatabases_list_tuple()
        {
            var repo = await RepositoryGenerator.Custom<ISpDatabase_value_tuple>(connection);
            var r = await repo.sp_databases();
            Assert.NotEmpty(r);
            foreach (var db in r)
            {
                Assert.NotEmpty(db.DATABASE_NAME);
                Assert.True(db.DATABASE_SIZE > 0);
            }
        }

        [Fact]
        public async Task SpDatabases_first_tuple()
        {
            var repo = await RepositoryGenerator.Custom<ISpDatabase_value_tuple>(connection);
            var db = await repo.sp_databases_first();
            Assert.NotEmpty(db.DATABASE_NAME);
            Assert.True(db.DATABASE_SIZE > 0);
        }

        [Fact]
        public async Task SelectMe()
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
create proc #select_me
    @id int,
    @value varchar(500)
as
begin
select  @id [Id], @value [myvalue]
end";
            await cmd.ExecuteNonQueryAsync();

            cmd.Dispose();

            var repo = await RepositoryGenerator.Custom<IIntRepository>(connection);
            var entity = new TestIntKeyEntity(99, "vvvv");
            TestIntKeyEntity r = await repo.SelectMe(entity);
            Console.WriteLine(r);
        }

        public interface IAA
        {
            Task<(string, string)> sp_tables();
        }

        public interface IBB
        {
            Task<string> sp_helptext(string objname);
        }

        public interface ICC : IAA, IBB
        {
            Task<(string, string)> sp_stored_procedures();
        }

        public interface IDD : IAA { }

        public interface IEE : IDD, IBB { }

        public interface IFF : IEE
        {
            Task<List<(string, string)>> sp_stored_procedures();
        }

        [Fact]
        public async Task CreateExtend()
        {
            var repo = await RepositoryGenerator.Custom<ICC>(connection);
            var repo2 = await RepositoryGenerator.Custom<IFF>(connection);
        }

        [Fact]
        public async Task Extends2()
        {
            var repo = await RepositoryGenerator.Custom<ICC>(connection);
            var r0 = await repo.sp_stored_procedures();
            Assert.False(string.IsNullOrEmpty(r0.Item1));
            Assert.False(string.IsNullOrEmpty(r0.Item2));

            var r1 = await repo.sp_helptext("sp_helptext");
            Assert.False(string.IsNullOrEmpty(r1));
        }

        [Fact]
        public async Task ExtendsExtends()
        {
            var repo = await RepositoryGenerator.Custom<IFF>(connection);
            var r0 = await repo.sp_stored_procedures();
            Assert.NotEmpty(r0);

            foreach (var e in r0)
            {
                Assert.False(string.IsNullOrEmpty(e.Item1));
                Assert.False(string.IsNullOrEmpty(e.Item2));
            }

            var r1 = await repo.sp_helptext("sp_helptext");
            Assert.False(string.IsNullOrEmpty(r1));
        }

        public struct SII
        {
            public int A { get; }
            public int B { get; }

            public SII(int a, int b)
            {
                A = a;
                B = b;
            }
        }

        public class EnumerableWrapper  
        {
            public IEnumerable<SII> Value { get; }

            public EnumerableWrapper(IEnumerable<SII> value)
            {
                Value = value;
            }
        }

        public interface ICustomEnumerable
        {
            Task PSSI(EnumerableWrapper value);
        }

        public interface ICustomEnumerable2
        {
            Task PSSI(IEnumerable<SII> value);
        }

        [Fact]
        public async Task TableType()
        {
            var cmd = connection.CreateCommand();

            cmd.CommandText = "drop proc if exists PSSI;";
            await cmd.ExecuteNonQueryAsync();

            cmd.CommandText = "drop type if exists SII;";
            await cmd.ExecuteNonQueryAsync();

            cmd.CommandText = "create type SII as table (a int,b int);";
            await cmd.ExecuteNonQueryAsync();


            cmd.CommandText = @"
create proc PSSI 
@value SII readonly
as
if (select count(*) from @value) = 0
throw 54321, 'list > 0', 1
";
            await cmd.ExecuteNonQueryAsync();

            try
            {
                
                var l = new List<SII>();
                l.Add(new SII(1, 2));
                l.Add(new SII(3, 4));
                var w = new EnumerableWrapper(l);

                var repo = await RepositoryGenerator.Custom<ICustomEnumerable>(connection);
                await repo.PSSI(new EnumerableWrapper(l));

                var repo2 = await RepositoryGenerator.Custom<ICustomEnumerable2>(connection);
                await repo2.PSSI(l);
            }
            finally
            {
                cmd.CommandText = "drop proc if exists PSSI;";
                await cmd.ExecuteNonQueryAsync();

                cmd.CommandText = "drop type if exists SII;";
                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
