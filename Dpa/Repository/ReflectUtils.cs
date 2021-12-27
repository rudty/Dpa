using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;

namespace Dpa.Repository
{
    internal readonly struct RepositoryPropertyNameInfo
    {
        public readonly List<string> PrimaryKeyPropertyNames;

        public readonly List<string> PropertyNames;

        public RepositoryPropertyNameInfo(List<string> primaryKeyPropertyName, List<string> propertyNames)
        {
            PrimaryKeyPropertyNames = primaryKeyPropertyName;
            PropertyNames = propertyNames;
        }
    }

    internal static class ReflectUtils
    {
        public static RepositoryPropertyNameInfo GetRepositoryPropertyInfo(Type objectType)
        {
            List<string> primaryKeyPropertyNames = new List<string>();
            List<string> propertyNames = new List<string>();
            foreach (PropertyInfo propertyInfo in objectType.GetProperties())
            {
                propertyNames.Add(propertyInfo.Name);
                foreach (CustomAttributeData attributeData in propertyInfo.CustomAttributes)
                {
                    if (attributeData.AttributeType == typeof(KeyAttribute))
                    {
                        primaryKeyPropertyNames.Add(propertyInfo.Name);
                    }
                }
            }

            if (primaryKeyPropertyNames.Count == 0)
            {
                throw new Exception("cannot found primarykey in " + objectType.FullName);
            }

            return new RepositoryPropertyNameInfo(primaryKeyPropertyNames, propertyNames);
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

            return new StringBuilder(name);
        }
    }
}
