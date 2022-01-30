﻿using System;
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
            EntityCollection<PropertyInfo> propertyInfo = classType.GetMappingProperties();
            EntityCollection<FieldInfo> fieldInfos = classType.GetMappingFields();
            columns = new Dictionary<ColumnName, FieldAndPropertyMemberMap>(fieldInfos.Count + propertyInfo.Count);
            baseDelegate = new Dapper.DefaultTypeMap(classType);

            foreach (Entity<PropertyInfo> member in propertyInfo)
            {
                if (member.Info.GetSetMethod(nonPublic: true) == null)
                {
                    continue;
                }

                ColumnName c = member.ColumnAttributeName;
                if (c != null)
                {
                    columns.TryAdd(c, new FieldAndPropertyMemberMap(c, member.Info));
                }
            }

            foreach (Entity<FieldInfo> member in fieldInfos)
            {
                ColumnName c = member.ColumnAttributeName;
                if (c != null)
                {
                    columns.TryAdd(c, new FieldAndPropertyMemberMap(c, member.Info));
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
