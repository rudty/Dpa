using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Dpa.Repository.Implements.Types
{
    internal static class TypeExtension
    {
        private const BindingFlags accessBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        private static bool ReturnTrue1<E>(E _)
        {
            return true;
        }

        internal static Entity<FieldInfo> GetMappingFields(this Type t, Func<Column<FieldInfo>, bool> selector = null)
        {
            if (selector is null)
            {
                selector = ReturnTrue1;
            }

            FieldInfo[] fieldInfos = t.GetFields(accessBindingFlags);
            Entity<FieldInfo> l = new Entity<FieldInfo>(fieldInfos.Length);
            foreach (FieldInfo f in fieldInfos)
            {
                if (f.GetCustomAttribute<NotMappedAttribute>() != null)
                {
                    continue;
                }

                Column<FieldInfo> e = Column<FieldInfo>.New(f);
                if (selector(e))
                {
                    l.Add(e);
                }
            }

            return l;
        }

        internal static Entity<PropertyInfo> GetMappingProperties(this Type t, Func<Column<PropertyInfo>, bool> selector = null)
        {
            if (selector is null)
            {
                selector = ReturnTrue1;
            }

            PropertyInfo[] props = t.GetProperties(accessBindingFlags);

            Entity<PropertyInfo> l = new Entity<PropertyInfo>(props.Length);
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

                Column<PropertyInfo> e = Column<PropertyInfo>.New(p);
                if (selector(e))
                {
                    l.Add(e);
                }
            }

            return l;
        }

        internal static Entity<ParameterInfo> GetMappingParameters(this MethodInfo m)
        {
            ParameterInfo[] parameterInfos = m.GetParameters();

            Entity<ParameterInfo> l = new Entity<ParameterInfo>(parameterInfos.Length);
            foreach (ParameterInfo f in parameterInfos)
            {
                l.Add(Column<ParameterInfo>.New(f));
            }

            return l;
        }
    }
}
