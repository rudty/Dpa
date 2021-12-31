
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Test.Entity
{
    [System.ComponentModel.DataAnnotations.Schema.Table(TableName)]
    public class TestIntKeyEntity
    {
        public  const string TableName = "inttable";

        public const string SqliteCreateTableQuery = @"
create table " + TableName + @"(
    id int,
    Value text
);";

        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }

        public string Value { get; set; }

        public TestIntKeyEntity() { }

        public TestIntKeyEntity(int id, string value)
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

            TestIntKeyEntity otherEntity = obj as TestIntKeyEntity;
            return otherEntity.Id == Id && otherEntity.Value == Value;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public static TestIntKeyEntity SelectEntityFirst(DbConnection connection, int id)
        {
            return SelectEntity(connection, id).First();
        }

        public static List<TestIntKeyEntity> SelectEntity(DbConnection connection, params int[] id)
        {
            using DbCommand cmd = connection.CreateCommand();
            cmd.CommandText = $"select id, value from {TableName} where id in ({string.Join(',', id)});";
            DbDataReader reader = cmd.ExecuteReader();

            List<TestIntKeyEntity> r = new List<TestIntKeyEntity>();
            while (reader.Read())
            {
                r.Add(new TestIntKeyEntity(reader.GetInt32(0), reader.GetString(1)));
            }

            return r;
        }
    }
}
