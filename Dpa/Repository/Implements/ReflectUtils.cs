using Dapper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;

namespace Dpa.Repository
{
    internal readonly struct RepositoryColumn
    {
        /// <summary>
        /// C# property
        /// </summary>
        public readonly string PropertyName;

        /// <summary>
        /// db column
        /// </summary>
        public readonly string ColumnName;

        public RepositoryColumn(string propertyName, string columnName)
        {
            PropertyName = propertyName;
            ColumnName = columnName;
        }
    }

    internal readonly struct RepositoryPropertyNameInfo
    {
        public readonly string TableName;

        public readonly List<RepositoryColumn> PrimaryKeyPropertyNames;

        public readonly List<RepositoryColumn> PropertyNames;

        public RepositoryPropertyNameInfo(string tableName, List<RepositoryColumn> primaryKeyPropertyName, List<RepositoryColumn> propertyNames)
        {
            TableName = tableName;
            PrimaryKeyPropertyNames = primaryKeyPropertyName;
            PropertyNames = propertyNames;
        }
    }

    internal static class ReflectUtils
    {
        public const BindingFlags TypeMapDefaultBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly Type[] supportAttributeTypes = new Type[]
        {
            typeof(ColumnAttribute),
            typeof(NotMappedAttribute),
        };

        public static bool IsPrimitiveLike(Type t)
        {
            if (t.IsPrimitive)
            {
                return true;
            }

            if (t == typeof(string))
            {
                return true;
            }

            if (t == typeof(DateTime))
            {
                return true;
            }

            if (t == typeof(TimeSpan))
            {
                return true;
            }

            if (t == typeof(System.Numerics.BigInteger))
            {
                return true;
            }

            if (t == typeof(decimal))
            {
                return true;
            }

            return false;
        }

        public static bool HasEntityAttribute(Type type)
        {
            PropertyInfo[] propertyInfo = type.GetProperties(TypeMapDefaultBindingFlags);
            foreach (PropertyInfo p in propertyInfo)
            {
                object[] attrs = p.GetCustomAttributes(true);
                foreach (object attr in attrs)
                {
                    Type attrType = attr.GetType();
                    foreach (Type supportAttrType in supportAttributeTypes)
                    {
                        if (attrType == supportAttrType)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public static RepositoryPropertyNameInfo GetRepositoryPropertyInfo(Type objectType)
        {
            PropertyInfo[] propertyInfos = objectType.GetProperties();
            List<RepositoryColumn> primaryKeyPropertyNames = new List<RepositoryColumn>(propertyInfos.Length);
            List<RepositoryColumn> propertyNames = new List<RepositoryColumn>(propertyInfos.Length);
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                NotMappedAttribute notMappedAttr = propertyInfo.GetCustomAttribute<NotMappedAttribute>();
                if (notMappedAttr != null)
                {
                    continue;
                }

                ColumnAttribute columnAttr = propertyInfo.GetCustomAttribute<ColumnAttribute>();
                string columnName = propertyInfo.Name;
                if (columnAttr != null)
                {
                    columnName = columnAttr.Name;
                }

                RepositoryColumn column = new RepositoryColumn(propertyInfo.Name, columnName);
                propertyNames.Add(column);

                KeyAttribute keyAttr = propertyInfo.GetCustomAttribute<KeyAttribute>();
                if (keyAttr != null)
                {
                    primaryKeyPropertyNames.Add(column);
                }
            }

            if (primaryKeyPropertyNames.Count == 0)
            {
                throw new Exception("cannot found primarykey in " + objectType.FullName);
            }

            string tableName = GetTableName(objectType);
            return new RepositoryPropertyNameInfo(tableName, primaryKeyPropertyNames, propertyNames);
        }

        public static string GetFullName(Type type)
        {
            return GetReflectName(type, t => t.FullName, "<", ">").ToString();
        }

        public static string GetTableName(Type type)
        {
            TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>();
            if (!(tableAttribute is null))
            {
                return tableAttribute.Name;
            }

            return GetReflectName(type, t => t.Name, "_", "_").ToString();
        }

        private static StringBuilder GetReflectName(Type type, Func<Type, string> getName, string beginGeneric, string endGeneric)
        {
            string name = getName(type);
            if (type.IsGenericType)
            {
                int divIndex = name.IndexOf('`');
                StringBuilder typeName = new StringBuilder(name.Substring(0, divIndex));
                Type[] genericArguments = type.GetGenericArguments();
                typeName.Append(beginGeneric);

                for (int i = 0; i < genericArguments.Length; ++i)
                {
                    if (i > 0)
                    {
                        typeName.Append(',');
                    }

                    StringBuilder argName = GetReflectName(
                        genericArguments[i], 
                        getName, 
                        beginGeneric, 
                        endGeneric);
                    typeName.Append(argName);
                }
                typeName.Append(endGeneric);

                return typeName;
            }

            return new StringBuilder(name).Replace('+', '.');
        }
    }
}
