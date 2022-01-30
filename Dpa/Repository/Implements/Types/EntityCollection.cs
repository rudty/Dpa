using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dpa.Repository.Implements.Types
{
    internal class EntityCollection<T> : List<Entity<T>>
    {
        internal EntityCollection(int capacity = 0) : base(capacity)
        {
        }

        public IEnumerable<Entity<T>> GetPrimaryKeys()
        {
            return this.Where(e => e.IsPrimaryKey);
        }

        public IEnumerable<Entity<T>> GetNotPkColumns()
        {
            return this.Where(e => (false == e.IsPrimaryKey));
        }

        public IEnumerable<Type> GetEntityTypes()
        {
            return this.Select(e => e.ColumnType);
        }

        public IEnumerable<Type> GetMemberypes()
        {
            return this.Select(e => e.MemberType);
        }
    }
}
