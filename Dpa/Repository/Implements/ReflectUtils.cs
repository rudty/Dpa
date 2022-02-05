using Dpa.Repository.Implements.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Reflection;
using System.Text;

namespace Dpa.Repository
{
    internal static class ReflectUtils
    {
        private static readonly ConcurrentDictionary<Type, bool> registeredTypeMap = new ConcurrentDictionary<Type, bool>();

        public static void SetType(Type type)
        {
            if (type.IsPrimitive)
            {
                return;
            }

            if (IsDbTypeExists(type))
            {
                return;
            }

            if (registeredTypeMap.TryAdd(type, true))
            {
                TypeMap m = new TypeMap(type);
                Dapper.SqlMapper.SetTypeMap(type, m);
            }
        }

        public const BindingFlags TypeMapDefaultBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly Type[] supportAttributeTypes = new Type[]
        {
            typeof(ColumnAttribute),
            typeof(NotMappedAttribute),
        };

        public static bool IsDbTypeExists(Type t)
        {
#pragma warning disable CS0618
            // https://github.com/DapperLib/Dapper/blob/b272cc664d933b4b65703d26a79272d549576dff/Dapper/SqlMapper.cs
            // db 
            DbType? d = Dapper.SqlMapper.LookupDbType(t, "_", false, out Dapper.SqlMapper.ITypeHandler h);
            if (d.HasValue && d != DbType.Object)
            {
                return true;
            }

            if (h != null)
            {
                return true;
            }

            return false;
#pragma warning restore CS0618
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
