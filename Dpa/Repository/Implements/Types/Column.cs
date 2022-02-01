using Dpa.Repository.Implements.Runtime;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Reflection.Emit;

namespace Dpa.Repository.Implements.Types
{
    internal readonly struct Column<T>
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

        /// <summary>
        /// MemberType => ColumnType 으로 변환하는 메서드
        /// </summary>
        public readonly Action<ILGenerator> MemberToColumn;

        public readonly T Info;

        private Column(
            string columnAttributeName, 
            string memberName, 
            string entityName, 
            Type entityType, 
            Type memberType, 
            bool isPrimaryKey,
            Action<ILGenerator> memberToColumn,
            T info)
        {
            ColumnAttributeName = columnAttributeName;
            MemberName = memberName;
            ColumnName = entityName;
            ColumnType = entityType;
            MemberType = memberType;
            IsPrimaryKey = isPrimaryKey;
            MemberToColumn = memberToColumn;
            Info = info;
        }

        public static Type GetMemberType<TMember>(TMember t) where TMember : MemberInfo
        {
            switch (t.MemberType)
            {
                case MemberTypes.Field:
                    return (t as FieldInfo).FieldType;
                case MemberTypes.Property:
                    return (t as PropertyInfo).PropertyType;
                default:
                    throw new NotSupportedException("support field, property");
            }
        }

        internal static Column<TMember> New<TMember>(TMember p) where TMember : MemberInfo
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

            Type memberType = GetMemberType(p);
            (Type entityType, Action<ILGenerator> convert) = ToEntityType(memberType);

            return new Column<TMember>(
                columnAttributeName,
                memberName,
                entityName,
                entityType,
                memberType,
                pk,
                convert,
                p);
        }

        internal static Column<ParameterInfo> New(ParameterInfo p)
        {
            string columnAttributeName = GetColumnAttributeName(p);
            string memberName = p.Name;
            string entityName = columnAttributeName;
            if (entityName is null)
            {
                entityName = memberName;
            }

            Type memberType = p.ParameterType;
            (Type entityType, Action<ILGenerator> convert) = ToEntityType(memberType);
            return new Column<ParameterInfo>(
                columnAttributeName,
                memberName,
                entityName,
                entityType,
                memberType,
                false,
                convert,
                p);
        }

        private static readonly Type IEnumerableClass = typeof(System.Collections.Generic.IEnumerable<>);

        private static Type GetInterface(Type t, Type findInterface)
        {
            if (t == findInterface)
            {
                return t;
            }

            if (findInterface.IsGenericType)
            {
                if (t.IsGenericType && 
                    t.GetGenericTypeDefinition() == findInterface)
                {
                    return t;
                }

                foreach (Type inter in t.GetInterfaces())
                {
                    if (inter == findInterface)
                    {
                        return inter;
                    }

                    if (inter.IsGenericType)
                    {
                        if (inter.GetGenericTypeDefinition() == findInterface)
                        {
                            return inter;
                        }
                    }
                }
            }

            foreach (Type inter in t.GetInterfaces())
            {
                if (inter.IsGenericType)
                {
                    continue;
                }

                if (inter == findInterface)
                {
                    return inter;
                }
            }
            return null;
        }

        private static (Type, Action<ILGenerator>) ToEntityType(Type memberType)
        {
            Type enumerableInterface = GetInterface(memberType, IEnumerableClass);

            if (enumerableInterface != null)
            {
                Type enumerableGenericArgument = enumerableInterface.GetGenericArguments()[0];
                if (!ReflectUtils.IsDbTypeExists(enumerableGenericArgument))
                {
                    return (typeof(Dapper.SqlMapper.ICustomQueryParameter), (il) =>
                    {
                        RuntimeTypeGenerator.CreateDataTableParameterFromEntityInline(il, memberType);
                    });
                }
            }
            return (memberType, null);
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
