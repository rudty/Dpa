using System;
using System.Collections.Generic;
using System.Linq;

namespace Dpa.Repository.Implements.Types
{
    internal class Entity<T> : List<Column<T>>
    {
        internal Entity(int capacity = 0) : base(capacity)
        {
        }

        public IEnumerable<Column<T>> GetPrimaryKeys()
        {
            return this.Where(e => e.IsPrimaryKey);
        }

        public IEnumerable<Column<T>> GetNotPkColumns()
        {
            return this.Where(e => (false == e.IsPrimaryKey));
        }

        public IEnumerable<Type> GetEntityTypes()
        {
            return this.Select(e => e.ColumnType);
        }

        public IEnumerable<Type> GetMemberTypes()
        {
            return this.Select(e => e.MemberType);
        }
    }
}
