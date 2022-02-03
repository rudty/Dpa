
using System;

namespace Test.Entity
{
    [System.ComponentModel.DataAnnotations.Schema.Table(TableName)]
    public class TestIntKeyEntity : IComparable<TestIntKeyEntity>
    {
        public const string TableName = "[#inttable]";

        public const string CreateTableQuery = @"
create table " + TableName + @"(
    myid int,
    myvalue varchar(500)
);";

        [System.ComponentModel.DataAnnotations.Key]
        public int myId { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.Column("myvalue")]
        public string Value { get; set; }


        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public int NotMappedValue { get; set; } = 333;

        public TestIntKeyEntity() { }

        public TestIntKeyEntity(int id, string value)
        {
            myId = id;
            Value = value;
        }
        
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            TestIntKeyEntity otherEntity = obj as TestIntKeyEntity;
            return otherEntity.myId == myId && otherEntity.Value == Value;
        }

        public override int GetHashCode()
        {
            return myId;
        }
        public int CompareTo(TestIntKeyEntity other)
        {
            if (myId != other.myId)
            {
                return myId.CompareTo(other.myId);
            }

            return Value.CompareTo(other.Value);
        }
    }
}
