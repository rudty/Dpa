using System;
using System.Reflection;

namespace Dpa.Repository.Implements.Types
{
    internal readonly struct DataAccessType
    {
        /// <summary>
        /// db 컬럼이름
        /// </summary>
        public readonly string EntityName;

        /// <summary>
        /// db에 넣을 타입 이름(dapper에 넣을)
        /// </summary>
        public readonly Type EntityType;

        /// <summary>
        /// 실제 타입
        /// </summary>
        public readonly Type MemberType;

        internal DataAccessType(string entityName, Type entityType, Type memberType)
        {
            EntityName = entityName;
            EntityType = entityType;
            MemberType = memberType;
        }
    }

    internal readonly struct DataAccess<T>
    {
        public readonly DataAccessType DataAccessType;
        public readonly T Info;

        private DataAccess(string entityName, Type entityType, Type memberType, T info)
        {
            DataAccessType = new DataAccessType(entityName, entityType, memberType);
            Info = info;
        }

        internal DataAccess<PropertyInfo> New(PropertyInfo p)
        {
            string columnName = p.GetColumnName();
            Type memberType = p.PropertyType;
            Type entityType = ToEntityType(memberType);
            return new DataAccess<PropertyInfo>(columnName, entityType, memberType, p);
        }

        internal DataAccess<ParameterInfo> New(ParameterInfo p)
        {
            string columnName = p.Name;
            Type memberType = p.ParameterType;
            Type entityType = ToEntityType(memberType);
            return new DataAccess<ParameterInfo>(columnName, entityType, memberType, p);
        }


        private static Type ToEntityType(Type memberType)
        {
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(memberType))
            {
                return typeof(Dapper.SqlMapper.ICustomQueryParameter);
            }

            return memberType;
        }

    }
}
