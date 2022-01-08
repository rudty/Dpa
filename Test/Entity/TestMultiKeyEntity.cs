using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Test.Entity
{
    public class MultiKey : IComparable<MultiKey>
    {
        [System.ComponentModel.DataAnnotations.Key]
        public int Id1 { get; set; }

        [System.ComponentModel.DataAnnotations.Key]
        public string Id2 { get; set; }

        public MultiKey() { }

        public MultiKey(int id1, string id2)
        {
            Id1 = id1;
            Id2 = id2;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            MultiKey otherEntity = obj as MultiKey;
            return otherEntity.Id1 == Id1 && otherEntity.Id2 == Id2;
        }

        public override int GetHashCode()
        {
            return Id1 * 10000 + Id2.GetHashCode();
        }

        public int CompareTo(MultiKey other)
        {
            if (other is null)
            {
                return 1;
            }

            if (Id1 != other.Id1)
            {
                return Id1.CompareTo(other.Id1);
            }

            return Id2.CompareTo(other.Id2);
        }
    }

    [System.ComponentModel.DataAnnotations.Schema.Table(TableName)]
    public class TestMultiKeyEntity : MultiKey, IComparable<TestMultiKeyEntity>
    {
        public const string TableName = "[#multikeytable]";

        public const string CreateTableQuery = @"
create table " + TableName + @"(
    id1 int,
    id2 varchar(500),
    Value varchar(500)
);";

        public string Value { get; set; }

        public TestMultiKeyEntity() { }

        public TestMultiKeyEntity(int id1, string id2, string value)
            : base(id1, id2)
        {
            Value = value;
        }

        public MultiKey ToKey()
        {
            return new MultiKey(Id1, Id2);
        }

        public int CompareTo(TestMultiKeyEntity other)
        {
            if (other is null)
            {
                return 1;
            }

            if (Id1 != other.Id1)
            {
                return Id1.CompareTo(other.Id1);
            }

            if (Id2 != other.Id2)
            {
                return Id2.CompareTo(other.Id2);
            }

            return Value.CompareTo(other.Value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            TestMultiKeyEntity otherEntity = obj as TestMultiKeyEntity;
            return otherEntity.Id1 == Id1 && otherEntity.Id2 == Id2 && otherEntity.Value == Value;
        }

        public override int GetHashCode()
        {
            return Id1 * 10000 + Id2.GetHashCode();
        }
    }
}
