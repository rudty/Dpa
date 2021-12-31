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

    public interface IHello
    {
        [Query("select * from " + TestIntKeyEntity.TableName + " where id = @id")]
        Task<TestIntKeyEntity> GetHello(int id);

        Task<MyDateTime> CustomTestDateProcedure();

        Task<IEnumerable<SysTables>> CustomTestTableSysTablesProcedure(string tablename);
    }

    public class CustomTest : IDisposable
    {
        private DbConnection connection;

        public CustomTest()
        {
            connection = new SqlConnection("server=localhost;Integrated Security=SSPI; database=test");
            connection.Open();

            DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = "drop table if exists " + TestIntKeyEntity.TableName;
            cmd.ExecuteNonQuery();

            cmd.CommandText = TestIntKeyEntity.SqliteCreateTableQuery;
            cmd.ExecuteNonQuery();

            cmd.CommandText = $"insert into {TestIntKeyEntity.TableName} values(1, 'a'), (2, 'b'), (3, 'c'), (999, 'd'), (999, 'e'), (999, 'f');";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"drop proc if exists CustomTestDateProcedure;";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
create proc CustomTestDateProcedure
as
begin
select getdate() as [NowDate]
end";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"drop proc if exists CustomTestTableSysTablesProcedure;";
            cmd.ExecuteNonQuery();

            cmd.CommandText = @"
create proc CustomTestTableSysTablesProcedure
@tablename varchar(max)
as
begin
select type_desc, object_id from sys.tables where [name] = @tablename 
end";
            cmd.ExecuteNonQuery();

            cmd.Dispose();
        }

        public void Dispose()
        {
            connection.Dispose();
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
            var repo = await RepositoryGenerator.Custom<IHello>(connection);
            var r = await repo.CustomTestDateProcedure();
            Assert.True(r.NowDate.Ticks > 0);
        }

        [Fact]
        public async Task SysTable()
        {
            var repo = await RepositoryGenerator.Custom<IHello>(connection);
            var r = await repo.CustomTestTableSysTablesProcedure("inttable");
            var firstResult = r.First();
            Assert.True(firstResult.object_id > 0);
            Assert.False(string.IsNullOrEmpty(firstResult.type_desc));
        }
    }
}
