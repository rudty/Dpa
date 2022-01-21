using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using ColumnName = System.String;

namespace Dpa.Repository.Implements.Types
{
    internal class TypeMap : Dapper.SqlMapper.ITypeMap
    {
        internal const BindingFlags TypeMapDefaultBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly ConcurrentDictionary<Type, bool> registeredTypeMap = new ConcurrentDictionary<Type, bool>();

        public static void SetType(Type type)
        {
            if (ReflectUtils.IsDbTypeExists(type))
            {
                return;                    
            }

            if (registeredTypeMap.TryAdd(type, true))
            {
                TypeMap m = new TypeMap(type);
                Dapper.SqlMapper.SetTypeMap(type, m);
            }
        }


        private readonly Dapper.DefaultTypeMap baseDelegate;
        private readonly Dictionary<ColumnName, FieldAndPropertyMemberMap> columns;

        public TypeMap(Type classType)
        {
            PropertyInfo[] propertyInfo = classType.GetProperties(TypeMapDefaultBindingFlags);
            FieldInfo[] fieldInfos = classType.GetFields(TypeMapDefaultBindingFlags);
            columns = new Dictionary<ColumnName, FieldAndPropertyMemberMap>(fieldInfos.Length + propertyInfo.Length);
            baseDelegate = new Dapper.DefaultTypeMap(classType);

            foreach (PropertyInfo member in propertyInfo)
            {
                if (member.GetSetMethod(nonPublic: true) == null)
                {
                    continue;
                }
                
                ColumnAttribute columnAttribute = member.GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute != null &&
                    false == string.IsNullOrEmpty(columnAttribute.Name))
                {
                    columns.TryAdd(columnAttribute.Name, new FieldAndPropertyMemberMap(columnAttribute.Name, member));
                }
            }

            foreach (FieldInfo member in fieldInfos)
            {
                ColumnAttribute columnAttribute = member.GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute != null &&
                    false == string.IsNullOrEmpty(columnAttribute.Name))
                {
                    columns.TryAdd(columnAttribute.Name, new FieldAndPropertyMemberMap(columnAttribute.Name, member));
                }
            }
        }
        ConstructorInfo Dapper.SqlMapper.ITypeMap.FindConstructor(string[] names, Type[] types)
        {
            return baseDelegate.FindConstructor(names, types);
        }

        ConstructorInfo Dapper.SqlMapper.ITypeMap.FindExplicitConstructor()
        {
            return baseDelegate.FindExplicitConstructor();
        }

        Dapper.SqlMapper.IMemberMap Dapper.SqlMapper.ITypeMap.GetConstructorParameter(ConstructorInfo constructor, string columnName)
        {
            return baseDelegate.GetConstructorParameter(constructor, columnName);
        }

        Dapper.SqlMapper.IMemberMap Dapper.SqlMapper.ITypeMap.GetMember(string columnName)
        {
            if (columns.TryGetValue(columnName, out FieldAndPropertyMemberMap m))
            {
                return m;
            }

            return baseDelegate.GetMember(columnName);
        }
    }
}
