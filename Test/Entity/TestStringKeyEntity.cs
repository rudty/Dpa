using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Test.Entity
{
    [System.ComponentModel.DataAnnotations.Schema.Table(TableName)]
    public class TestStringKeyEntity
    {
        public const string TableName = "stringtable";

        public const string SqliteCreateTableQuery = @"
create table " + TableName + @" (
    id text,
    Value int
);";


        [System.ComponentModel.DataAnnotations.Key]
        public string Id { get; set; }

        public int Value { get; set; }

        public TestStringKeyEntity() { }

        public TestStringKeyEntity(string id,  int value)
        {
            Id = id;
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            TestStringKeyEntity otherEntity = obj as TestStringKeyEntity;
            return otherEntity.Id == Id && otherEntity.Value == Value;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public static TestStringKeyEntity SelectEntityFirst(DbConnection connection, string id)
        {
            return SelectEntity(connection, id).First();
        }

        public static List<TestStringKeyEntity> SelectEntity(DbConnection connection, params string[] id)
        {
            using DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = $"select id, value from {TestStringKeyEntity.TableName} where id in ('{string.Join("','", id)}');";
            DbDataReader reader = cmd.ExecuteReader();
            List<TestStringKeyEntity> r = new List<TestStringKeyEntity>();
            while (reader.Read())
            {
                r.Add(new TestStringKeyEntity(reader.GetString(0), reader.GetInt32(1)));
            }
            return r;
        }
    }
}
