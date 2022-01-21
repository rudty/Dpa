using System;
using System.Reflection;

namespace Dpa.Repository.Implements.Types
{
    internal class FieldAndPropertyMemberMap : Dapper.SqlMapper.IMemberMap
    {
        public FieldAndPropertyMemberMap(string columnName, PropertyInfo property)
        {
            MustNotNull(columnName, nameof(columnName));
            MustNotNull(property, nameof(property));

            ColumnName = columnName;
            Property = property;
            MemberType = property.PropertyType;
        }

        public FieldAndPropertyMemberMap(string columnName, FieldInfo field)
        {
            MustNotNull(columnName, nameof(columnName));
            MustNotNull(field, nameof(field));

            ColumnName = columnName;
            Field = field;
            MemberType = field.FieldType;
        }

        private static void MustNotNull<T>(T value, string name)
        {
            if (value is null)
            {
                throw new ArgumentNullException(name);
            }
        }

        public string ColumnName { get; }

        public Type MemberType { get; }

        public PropertyInfo Property { get; } = null;

        public FieldInfo Field { get; } = null;

        public ParameterInfo Parameter { get; } = null;
    }
}

