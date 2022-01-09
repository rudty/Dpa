using Dpa.Repository;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Test.Helper;
using Xunit;

namespace Test
{
    public struct ValueEntity
    {
        [Key]
        public int A { get; set; }
        public int B { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            ValueEntity other = (ValueEntity)obj;
            return A == other.A && B == other.B;
        }

        public bool IsEmpty()
        {
            return A == 0 && B == 0;
        }

        public override int GetHashCode()
        {
            return A + B;
        }
    }

    public class ValueTypeTest : SqliteTestClass
    {
        public ValueTypeTest()
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "create table ValueEntity (A int, B int);";
                cmd.ExecuteNonQuery();
            }
        }

        public interface ITypeTest
        {
            [Query("insert into ValueEntity values(@A, @B);")]
            Task Insert(ValueEntity entity);
        }

        [Fact]
        public async Task ValueTypeCustom()
        {
            var repo = await RepositoryGenerator.Custom<ITypeTest>(connection);
            await repo.Insert(new ValueEntity()
            {
                A = 3,
                B = 2,
            });
        }

        [Fact]
        public async Task ValueTypeCrud()
        {
            var repo = await RepositoryGenerator.Default<ValueEntity, int>(connection);

            var insertId = 8884;
            var insertValue = new ValueEntity
            {
                A = insertId, B = 2
            };
            await repo.InsertRow(insertValue);

            var r0 = await repo.SelectRow(insertId);
            Assert.Equal(insertValue, r0);

            insertValue.B = 78;
            await repo.UpdateRow(insertValue);

            var r1 = await repo.SelectRow(insertId);
            Assert.Equal(insertValue, r1);

            await repo.DeleteRow(insertValue.A);

            var r2 = await repo.SelectRow(insertId);
            Assert.True(r2.IsEmpty());
        }
    }
}
