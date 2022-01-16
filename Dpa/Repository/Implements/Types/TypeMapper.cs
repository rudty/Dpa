using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using ColumnName = System.String;

namespace Dpa.Repository.Implements.Types
{
    internal class TypeMapper
    {
        public const BindingFlags TypeMapDefaultBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly ConcurrentDictionary<Type, bool> registeredTypeMap = new ConcurrentDictionary<Type, bool>();

        private readonly Dapper.CustomPropertyTypeMap propertyTypeMap;
        private readonly Dictionary<ColumnName, PropertyInfo> columnPropDictionary;

        public static void SetType(Type type)
        {
            if (ReflectUtils.IsPrimitiveLike(type))
            {
                return;
            }

            if (registeredTypeMap.TryAdd(type, true))
            {
                TypeMapper m = new TypeMapper(type);
                Dapper.SqlMapper.SetTypeMap(type, m.propertyTypeMap);
            }
        }

        public TypeMapper(Type classType)
        {
            PropertyInfo[] propertyInfo = classType.GetProperties(TypeMapDefaultBindingFlags);
            columnPropDictionary = new Dictionary<ColumnName, PropertyInfo>(propertyInfo.Length);
            propertyTypeMap = new Dapper.CustomPropertyTypeMap(classType, PropertySelector);

            for (int i = 0; i < propertyInfo.Length; ++i)
            {
                NotMappedAttribute notMappedAttribute = propertyInfo[i].GetCustomAttribute<NotMappedAttribute>();
                if (notMappedAttribute != null)
                {
                    continue;
                }

                ColumnAttribute columnAttribute = propertyInfo[i].GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute != null)
                {
                    columnPropDictionary.TryAdd(columnAttribute.Name, propertyInfo[i]);
                }
                else
                {
                    ColumnName propertyName = propertyInfo[i].Name;
                    columnPropDictionary.TryAdd(propertyName, propertyInfo[i]);
                    columnPropDictionary.TryAdd(propertyName.ToUpper(), propertyInfo[i]);
                }
            }
         }

        private PropertyInfo PropertySelector(Type classType, ColumnName columnName)
        {
            PropertyInfo prop;
            if (columnPropDictionary.TryGetValue(columnName, out prop))
            {
                return prop;
            }
           
            if (columnPropDictionary.TryGetValue(columnName.ToUpper(), out prop))
            {
                return prop;
            }

            return null;
        }

    }
}
