using Dpa.Repository;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Test.Entity;
using Xunit;

namespace Test.Helper
{
    internal class CrudTestHelper
    {
        public static int uniqueId = 64631;

        public static async Task TestAll(ICrudRepository<TestIntKeyEntity, int> repo)
        {
            await Row(repo);
            await Rows(repo);
        }

        public static async Task TestAll(ICrudRepository<TestMultiKeyEntity, MultiKey> repo)
        {
            await Row(repo);
            await Rows(repo);
        }

        private static async Task Row(ICrudRepository<TestIntKeyEntity, int> repo)
        {
            var insertId = Interlocked.Increment(ref uniqueId);
            var insertValue = new TestIntKeyEntity(insertId, "x" + insertId);
            await repo.InsertRow(insertValue);

            var r0 = await repo.SelectRow(insertId);
            Assert.Equal(insertValue, r0);

            insertValue.Value = "aaa" + insertId;
            await repo.UpdateRow(insertValue);

            var r1 = await repo.SelectRow(insertId);
            Assert.Equal(insertValue, r1);

            await repo.DeleteRow(insertValue.myId);

            var r2 = await repo.SelectRow(insertId);
            Assert.True(null == r2);
        }

        private static async Task Row(ICrudRepository<TestMultiKeyEntity, MultiKey> repo)
        {
            var insertId1 = Interlocked.Increment(ref uniqueId);
            var insertId2 = Interlocked.Increment(ref uniqueId).ToString();
            var insertValue = new TestMultiKeyEntity(insertId1, insertId2, "x" + insertId1 + "_" + insertId2);
            await repo.InsertRow(insertValue);

            var key = new MultiKey(insertId1, insertId2);
            var r0 = await repo.SelectRow(key);
            Assert.Equal(insertValue, r0);

            insertValue.Value = "aaa" + insertId1 + "_" + insertId2;
            await repo.UpdateRow(insertValue);

            var r1 = await repo.SelectRow(key);
            Assert.Equal(insertValue, r1);

            await repo.DeleteRow(key);

            var r2 = await repo.SelectRow(key);
            Assert.True(null == r2);
        }

        private static async Task Rows(ICrudRepository<TestIntKeyEntity, int> repo)
        {
            List<TestIntKeyEntity> insertValues = new List<TestIntKeyEntity>(100);
            for (int i = 0; i < 100; ++i)
            {
                var insertId = Interlocked.Increment(ref uniqueId);
                insertValues.Add(new TestIntKeyEntity(insertId, "x" + insertId + "_" + i.ToString("D4")));
            }

            await repo.Insert(insertValues);

            var l0 = await SelectValues(repo, insertValues.Select(e => e.myId));
            Assert.Equal(insertValues, l0);

            for (int i = 0; i < 100; ++i)
            {
                insertValues[i].Value = "y_" + i.ToString("D4");
            }

            await repo.Update(insertValues);

            var l1 = await SelectValues(repo, insertValues.Select(e => e.myId));
            Assert.Equal(insertValues, l1);

            await repo.Delete(insertValues.Select(e => e.myId));
            var l2 = await SelectValues(repo, insertValues.Select(e => e.myId));
            Assert.Empty(l2);
        }

        private static async Task Rows(ICrudRepository<TestMultiKeyEntity, MultiKey> repo)
        {
            List<TestMultiKeyEntity> insertValues = new List<TestMultiKeyEntity>(100);
            for (int i = 0; i < 100; ++i)
            {
                var insertId1 = Interlocked.Increment(ref uniqueId);
                var insertId2 = Interlocked.Increment(ref uniqueId).ToString();
                insertValues.Add(new TestMultiKeyEntity(insertId1, insertId2, "x" + insertId1 +"_" + insertId2 + "_" + i.ToString("D4")));
            }

            await repo.Insert(insertValues);

            var l0 = await SelectValues(repo, insertValues.Select(e => e.ToKey()));
            Assert.Equal(insertValues, l0);

            for (int i = 0; i < 100; ++i)
            {
                insertValues[i].Value = "y_" + i.ToString("D4");
            }

            await repo.Update(insertValues);

            var l1 = await SelectValues(repo, insertValues.Select(e => e.ToKey()));
            Assert.Equal(insertValues, l1);

            await repo.Delete(insertValues.Select(e => e.ToKey()));
            var l2 = await SelectValues(repo, insertValues.Select(e => e.ToKey()));
            Assert.Empty(l2);
        }


        private static async Task<List<T>> SelectValues<T, ID>(ICrudRepository<T, ID> repo, IEnumerable<ID> keys)
        {
            var selectTask = new List<Task<IReadOnlyCollection<T>>>();
            foreach (ID key in keys)
            {
                selectTask.Add(repo.Select(key)); 
            }

            IReadOnlyCollection<T>[] taskResult = await Task.WhenAll(selectTask);
            List<T> entities = new List<T>(taskResult.Length);

            foreach (IReadOnlyCollection<T> r in taskResult)
            {
                if (r.Count == 0) { }
                else if (r.Count == 1)
                {
                    entities.Add(r.First());
                }
                else
                {
                    throw new Exception("count != 0 or 1");
                }
            }

            return entities;
        }

    }
}
