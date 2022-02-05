
using Dpa.Repository.Implements.Runtime;
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

        /// <summary>
        /// ID 가 db 타입일때 return new { 아이디: 아이디 };
        /// ID 가 db 커스텀 클래스일때 return 입력 그대로
        /// </summary>
        public static Func<ID, object> GetDefaultIdQueryParameterBinder()
        {
            if (ReflectUtils.IsDbTypeExists(typeof(ID)))
            {
                return RuntimeTypeGenerator.CreateFunctionPrimaryKeyAnonymousEntity<T, ID>();
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
    }
}
