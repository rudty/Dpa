using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
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
        private const BindingFlags typeMapDefaultBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly ConcurrentDictionary<Type, bool> registeredTypeMap = new ConcurrentDictionary<Type, bool>();

        public static void SetTypeMap(Type type)
        {
            if (registeredTypeMap.TryAdd(type, true))
            {
                PropertyInfo[] propertyInfo = type.GetProperties(typeMapDefaultBindingFlags);
                if (propertyInfo.Any(p => p.GetCustomAttribute<ColumnAttribute>() != null))
                {
                    Dapper.CustomPropertyTypeMap typeMap = new Dapper.CustomPropertyTypeMap(type, PropertySelector);
                    Dapper.SqlMapper.SetTypeMap(type, typeMap);
                }
            }
        }

        private static PropertyInfo PropertySelector(Type classType, string columnName)
        {
            PropertyInfo[] propertyInfo = classType.GetProperties(typeMapDefaultBindingFlags);
            for (int i = 0; i < propertyInfo.Length; ++i)
            {
                ColumnAttribute columnAttribute = propertyInfo[i].GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute != null)
                {
                    if (columnAttribute.Name == columnName)
                    {
                        return propertyInfo[i];
                    }
                }
            }

            for (int i = 0; i < propertyInfo.Length; ++i)
            {
                if (propertyInfo[i].Name.ToUpper() == columnName.ToUpper())
                {
                    return propertyInfo[i];
                }
            }

            return null;
        }

        public static RepositoryPropertyNameInfo GetRepositoryPropertyInfo(Type objectType)
        {
            PropertyInfo[] propertyInfos = objectType.GetProperties();
            List<RepositoryColumn> primaryKeyPropertyNames = new List<RepositoryColumn>(propertyInfos.Length);
            List<RepositoryColumn> propertyNames = new List<RepositoryColumn>(propertyInfos.Length);
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
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

                    typeName.Append(GetFullName(genericArguments[i]));
                }
                typeName.Append(endGeneric);

                return typeName;
            }

            return new StringBuilder(name).Replace('+', '.');
        }
    }
}
