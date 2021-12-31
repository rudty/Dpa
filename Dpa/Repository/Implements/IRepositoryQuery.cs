
using System;
using System.Data;

namespace Dpa.Repository.Implements
{
    public interface IRepositoryQuery<T, ID>
    {
        QueryAndParameter<ID> Select { get; }
        QueryAndParameter<T> Insert { get; }
        QueryAndParameter<T> Update { get; }
        QueryAndParameter<ID> Delete { get; }
        CommandType CommandType { get; }

        public static Func<ID, object> GetDefaultIdQueryParameterBinder()
        {
            Type idType = typeof(ID);
            if (idType.IsPrimitive || idType == typeof(string))
            {
                return IdBinder;
            }

            return EntityBinder;
        }

        public static Func<T, object> GetDefaultEntityQueryParameterBinder()
        {
            return EntityBinder;
        }

        private static object EntityBinder<E>(E value)
        {
            return value;
        }

        private static object IdBinder<E>(E id)
        {
            return new { id };
        }
    }
}
