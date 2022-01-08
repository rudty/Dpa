using Dpa.Repository;
using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Test.Entity;

namespace Test.Helper
{
    public class SqlServerTestClass : IDisposable
    {
        protected DbConnection connection { get; }

        protected SqlServerTestClass()
        {
            connection = new SqlConnection("server=localhost;Integrated Security=SSPI; database=test; MultipleActiveResultSets=true;");
            connection.Open();

            DbCommand cmd = connection.CreateCommand();

            cmd.CommandText = TestIntKeyEntity.CreateTableQuery;
            cmd.ExecuteNonQuery();

            cmd.CommandText = TestMultiKeyEntity.CreateTableQuery;
            cmd.ExecuteNonQuery();

            Dapper.SqlMapper.AddTypeMap(typeof(string), System.Data.DbType.AnsiString);
        }

        public void Dispose()
        {
            connection.Close();
        }
    }
}
