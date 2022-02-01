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

        internal static EntityCollection<FieldInfo> GetMappingFields(this Type t, Func<Entity<FieldInfo>, bool> selector = null)
        {
            if (selector is null)
            {
                selector = ReturnTrue1;
            }

            FieldInfo[] fieldInfos = t.GetFields(accessBindingFlags);
            EntityCollection<FieldInfo> l = new EntityCollection<FieldInfo>(fieldInfos.Length);
            foreach (FieldInfo f in fieldInfos)
            {
                if (f.GetCustomAttribute<NotMappedAttribute>() != null)
                {
                    continue;
                }

                Entity<FieldInfo> e = Entity<FieldInfo>.New(f);
                if (selector(e))
                {
                    l.Add(e);
                }
            }

            return l;
        }

        internal static EntityCollection<PropertyInfo> GetMappingProperties(this Type t, Func<Entity<PropertyInfo>, bool> selector = null)
        {
            if (selector is null)
            {
                selector = ReturnTrue1;
            }

            PropertyInfo[] props = t.GetProperties(accessBindingFlags);

            EntityCollection<PropertyInfo> l = new EntityCollection<PropertyInfo>(props.Length);
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

                Entity<PropertyInfo> e = Entity<PropertyInfo>.New(p);
                if (selector(e))
                {
                    l.Add(e);
                }
            }

            return l;
        }

        internal static EntityCollection<ParameterInfo> GetMappingParameters(this MethodInfo m)
        {
            ParameterInfo[] parameterInfos = m.GetParameters();

            EntityCollection<ParameterInfo> l = new EntityCollection<ParameterInfo>(parameterInfos.Length);
            foreach (ParameterInfo f in parameterInfos)
            {
                l.Add(Entity<ParameterInfo>.New(f));
            }

            return l;
        }
    }
}
