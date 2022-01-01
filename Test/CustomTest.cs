using System;
using Xunit;
using Dpa.Repository;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using System.Threading.Tasks;
using Test.Entity;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace Test
{
    public class MyDateTime
    {
        public DateTime NowDate { get; set; }
    }

    public class SysTables
    {
        public string type_desc { get; set; }
        public int object_id { get; set; }
    }

    public class ParamObject
    {
        public int A { get; set; }

        public int B { get; set; }

        public string C { get; set; }

        // override object.Equals
        public override bool Equals(object obj)
        {

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            ParamObject other = (ParamObject)obj;
            return other.A == A && other.B == B && other.C == C;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            // TODO: write your implementation of GetHashCode() here
            return base.GetHashCode();
        }
    }

    public interface IHello
    {
        [Query("select * from " + TestIntKeyEntity.TableName + " where id = @id")]
        Task<TestIntKeyEntity> GetHello(int id);

        Task<MyDateTime> CustomTestDateProcedure();

        Task<List<SysTables>> CustomTestTableSysTablesProcedure(string tablename);

        Task<ParamObject> CustomObjectParams(ParamObject o);

        Task UpdateTestIntEntity(TestIntKeyEntity e);
    }

    [Collection("sqlserver")]
    public class CustomTest : IClassFixture<SqlServerDatabaseFixture>
    {
        private DbConnection connection;

        public CustomTest(SqlServerDatabaseFixture fixture)
        {
            this.connection = fixture.SqlServerConnection;
        }

        [Fact]
        public async Task Create()
        {
            var repo = await RepositoryGenerator.Custom<IHello>(connection);
            Assert.NotNull(repo);
        }

        [Fact]
        public async Task SelectHello1()
        {
            var repo = await RepositoryGenerator.Custom<IHello>(connection);
            var entity = await repo.GetHello(1);
            Assert.Equal(new TestIntKeyEntity(1, "a"), entity);

            entity = await repo.GetHello(2);
            Assert.Equal(new TestIntKeyEntity(2, "b"), entity);

            entity = await repo.GetHello(3);
            Assert.Equal(new TestIntKeyEntity(3, "c"), entity);
        }

        [Fact]
        public async Task GetDateProcedure()
        {
            var cmd = connection.CreateCommand();
            cmd.CommandText = @"drop proc if exists CustomTestDateProcedure;";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
create proc CustomTestDateProcedure
as
begin
select getdate() as [NowDate]
end";
            cmd.ExecuteNonQuery();

            cmd.Dispose();

            var repo = await RepositoryGenerator.Custom<IHello>(connection);
            var r = await repo.CustomTestDateProcedure();
            Assert.True(r.NowDate.Ticks > 0);
        }

        [Fact]
        public async Task SysTable()
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"drop proc if exists CustomTestTableSysTablesProcedure;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = @"
create proc CustomTestTableSysTablesProcedure
@tablename varchar(max)
as
begin
select type_desc, object_id from sys.tables where [name] = @tablename;
end";
                cmd.ExecuteNonQuery();
            }

            var repo = await RepositoryGenerator.Custom<IHello>(connection);
            List<SysTables> r = await repo.CustomTestTableSysTablesProcedure("inttable");
            var firstResult = r.First();
            Assert.True(firstResult.object_id > 0);
            Assert.False(string.IsNullOrEmpty(firstResult.type_desc));
        }

        [Fact]
        public async Task ObjectParameter()
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "drop proc if exists CustomObjectParams;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = $@"
create proc CustomObjectParams
@a int,
@b int,
@c varchar(max)
as
begin
select (@a + 1) as [A], (@b + 1) as [B], (@c + 'c') as [C];
end";
                cmd.ExecuteNonQuery();
            }

            var repo = await RepositoryGenerator.Custom<IHello>(connection);
            var p = new ParamObject()
            {
                A = 3,
                B = 2,
                C = "ab",
            };
            var r = await repo.CustomObjectParams(p);
            Assert.Equal(new ParamObject()
            {
                A = 4,
                B = 3,
                C = "abc",
            }, r);
        }

        [Fact]
        public async Task Update()
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "drop proc if exists UpdateTestIntEntity;";
                cmd.ExecuteNonQuery();

                cmd.CommandText = $@"
create proc UpdateTestIntEntity
@id int,
@value varchar(max)
as
begin
    update inttable set [value] = @value where [id] = @id;
end";
                cmd.ExecuteNonQuery();
            }

            TestIntKeyEntity update = new TestIntKeyEntity(13, "ZZZZ");
            IHello repo = await RepositoryGenerator.Custom<IHello>(connection);
            await repo.UpdateTestIntEntity(update);

            TestIntKeyEntity findEntity = TestIntKeyEntity.SelectEntityFirst(connection, 13);

            Assert.Equal(update, findEntity);
        }
    }
}
