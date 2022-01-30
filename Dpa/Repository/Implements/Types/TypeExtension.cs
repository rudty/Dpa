using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Dpa.Repository.Implements.Types
{
    internal static class TypeExtension
    {
        private const BindingFlags accessBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        internal static EntityCollection<FieldInfo> GetMappingFields(this Type t)
        {
            FieldInfo[] fieldInfos = t.GetFields(accessBindingFlags);

            EntityCollection<FieldInfo> l = new EntityCollection<FieldInfo>(fieldInfos.Length);
            foreach (FieldInfo f in fieldInfos)
            {
                if (f.GetCustomAttribute<NotMappedAttribute>() != null)
                {
                    continue;
                }

                l.Add(Entity<FieldInfo>.New(f));
            }

            return l;
        }

        internal static EntityCollection<PropertyInfo> GetMappingProperties(this Type t)
        {
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

                l.Add(Entity<PropertyInfo>.New(p));
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
