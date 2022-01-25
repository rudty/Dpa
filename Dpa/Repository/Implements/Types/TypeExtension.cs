using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Dpa.Repository.Implements.Types
{
    internal static class TypeExtension
    {
        private const BindingFlags accessBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        internal static string GetColumnAttributeName(this MemberInfo m)
        {
            ColumnAttribute attr = m.GetCustomAttribute<ColumnAttribute>();
            if (attr != null && false == string.IsNullOrEmpty(attr.Name))
            {
                return attr.Name;
            }

            return null;
        }

        internal static string GetColumnName(this MemberInfo m)
        {
            string columnAttrName = GetColumnAttributeName(m);
            if (columnAttrName is null)
            {
                return m.Name;
            }

            return columnAttrName;
        }

        internal static IReadOnlyList<FieldInfo> GetMappingFields(this Type t)
        {
            FieldInfo[] fieldInfos = t.GetFields(accessBindingFlags);

            List<FieldInfo> l = new List<FieldInfo>(fieldInfos.Length);
            foreach (FieldInfo f in fieldInfos)
            {
                if (f.GetCustomAttribute<NotMappedAttribute>() != null)
                {
                    continue;
                }

                l.Add(f);
            }

            return l;
        }

        internal static IReadOnlyList<PropertyInfo> GetMappingProperties(this Type t)
        {
            PropertyInfo[] props = t.GetProperties(accessBindingFlags);

            List<PropertyInfo> l = new List<PropertyInfo>(props.Length);
            foreach (PropertyInfo p in props)
            {
                if (p.GetCustomAttribute<NotMappedAttribute>() != null)
                {
                    continue;
                }

                MethodInfo getter = p.GetGetMethod(nonPublic: true);
                if (getter is null)
                {
                    continue;
                }

                l.Add(p);
            }

            return l;
        }
    }
}
