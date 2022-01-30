using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Dpa.Repository.Implements.Types
{
    internal readonly struct Entity<T>
    {
        /// <summary>
        /// colum attribute 사용했을때 이름
        /// </summary>
        public readonly string ColumnAttributeName;

        /// <summary>
        /// MemberType 변수 이름
        /// </summary>
        public readonly string MemberName;

        /// <summary>
        /// db 컬럼으로 사용할 이름
        /// </summary>
        public readonly string ColumnName;

        /// <summary>
        /// db에 넣을 타입 이름(dapper에 넣을)
        /// </summary>
        public readonly Type ColumnType;

        /// <summary>
        /// 실제 타입
        /// </summary>
        public readonly Type MemberType;

        /// <summary>
        /// Pk로 사용하는 컬럼 (기본 키로 사용)
        /// </summary>
        public readonly bool IsPrimaryKey;

        public readonly T Info;

        private Entity(string columnAttributeName, string memberName, string entityName, Type entityType, Type memberType, bool isPrimaryKey, T info)
        {
            ColumnAttributeName = columnAttributeName;
            MemberName = memberName;
            ColumnName = entityName;
            ColumnType = entityType;
            MemberType = memberType;
            IsPrimaryKey = isPrimaryKey;
            Info = info;
        }

        internal static Entity<PropertyInfo> New(PropertyInfo p)
        {
            string columnAttributeName = GetColumnAttributeName(p);
            string memberName = p.Name;
            string entityName = columnAttributeName;
            if (entityName is null)
            {
                entityName = memberName;
            }

            bool pk = false;
            KeyAttribute keyAttr = p.GetCustomAttribute<KeyAttribute>();
            if (keyAttr != null)
            {
                pk = true;
            }

            Type memberType = p.PropertyType;
            Type entityType = ToEntityType(memberType);

            return new Entity<PropertyInfo>(
                columnAttributeName,
                memberName,
                entityName,
                entityType,
                memberType,
                pk,
                p);
        }

        internal static Entity<ParameterInfo> New(ParameterInfo p)
        {
            string columnAttributeName = GetColumnAttributeName(p);
            string memberName = p.Name;
            string entityName = columnAttributeName;
            if (entityName is null)
            {
                entityName = memberName;
            }

            Type memberType = p.ParameterType;
            Type entityType = ToEntityType(memberType);
            return new Entity<ParameterInfo>(
                columnAttributeName,
                memberName,
                entityName,
                entityType,
                memberType,
                false,
                p);
        }

        internal static Entity<FieldInfo> New(FieldInfo f)
        {
            string columnAttributeName = GetColumnAttributeName(f);
            string memberName = f.Name;
            string entityName = columnAttributeName;
            if (entityName is null)
            {
                entityName = memberName;
            }

            bool pk = false;
            KeyAttribute keyAttr = f.GetCustomAttribute<KeyAttribute>();
            if (keyAttr != null)
            {
                pk = true;
            }

            Type memberType = f.FieldType;
            Type entityType = ToEntityType(memberType);
            return new Entity<FieldInfo>(
                columnAttributeName,
                memberName,
                entityName,
                entityType,
                memberType,
                pk,
                f);
        }

        private static Type ToEntityType(Type memberType)
        {
            //if (typeof(System.Collections.IEnumerable).IsAssignableFrom(memberType))
            //{
            //    return typeof(Dapper.SqlMapper.ICustomQueryParameter);
            //}

            return memberType;
        }

        private static string GetColumnAttributeName(MemberInfo m)
        {
            ColumnAttribute attr = m.GetCustomAttribute<ColumnAttribute>();
            if (attr != null && false == string.IsNullOrEmpty(attr.Name))
            {
                return attr.Name;
            }

            return null;
        }

        private static string GetColumnAttributeName(ParameterInfo m)
        {
            ColumnAttribute attr = m.GetCustomAttribute<ColumnAttribute>();
            if (attr != null && false == string.IsNullOrEmpty(attr.Name))
            {
                return attr.Name;
            }

            return null;
        }
    }
}
