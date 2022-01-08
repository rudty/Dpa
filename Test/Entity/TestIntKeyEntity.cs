
using System;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Test.Entity
{
    [System.ComponentModel.DataAnnotations.Schema.Table(TableName)]
    public class TestIntKeyEntity : IComparable<TestIntKeyEntity>
    {
        public const string TableName = "[#inttable]";

        public const string CreateTableQuery = @"
create table " + TableName + @"(
    id int,
    myvalue varchar(500)
);";

        [System.ComponentModel.DataAnnotations.Key]
        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.Column("myvalue")]
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
        public int CompareTo(TestIntKeyEntity other)
        {
            if (Id != other.Id)
            {
                return Id.CompareTo(other.Id);
            }

            return Value.CompareTo(other.Value);
        }
    }
}
