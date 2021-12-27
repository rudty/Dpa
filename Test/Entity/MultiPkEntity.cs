using System;
using System.Collections.Generic;
using System.Text;

namespace Test.Entity
{
    public class MultiPk
    {
        public int Id1 { get; set; }

        public string Id2 { get; set; }
        public MultiPk() { }

        public MultiPk(int id1, string id2)
        {
            Id1 = id1;
            Id2 = id2;
        }
    }

    [System.ComponentModel.DataAnnotations.Schema.Table(TableName)]
    public class MultiPkEntity
    {
        public const string TableName = "multipk";

        public const string SqliteCreateTableQuery = @"
create table " + TableName + @"(
    id1 int,
    id2 text,
    Value text
);";

        [System.ComponentModel.DataAnnotations.Key]
        public int Id1 { get; set; }

        [System.ComponentModel.DataAnnotations.Key]
        public string Id2 { get; set; }

        public string Value { get; set; }

        public MultiPkEntity() { }

        public MultiPkEntity(int id1, string id2, string value)
        {
            Id1 = id1;
            Id2 = id2;
            Value = value;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            MultiPkEntity otherEntity = obj as MultiPkEntity;
            return otherEntity.Id1 == Id1 && otherEntity.Id2 == Id2;
        }

        public override int GetHashCode()
        {
            return Id1.GetHashCode() + Id2.GetHashCode();
        }


    }
}
