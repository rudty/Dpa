
using Dpa.Repository.Implements.Runtime;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;

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
            if (ReflectUtils.IsPrimitiveLike(typeof(ID)))
            {
                return IdBinder;
            }

            return EntityBinder;
        }

        public static Func<T, object> GetDefaultEntityQueryParameterBinder()
        {
            if (ReflectUtils.HasEntityAttribute(typeof(T)))
            {
                return RuntimeTypeGenerator.CreateFunctionClonePropertyAnonymousEntity<T>();   
            }
            else
            {
                return EntityBinder;
            }
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
