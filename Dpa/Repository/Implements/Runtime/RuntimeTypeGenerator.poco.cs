using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Dpa.Repository.Implements.Runtime
{
    public static partial class RuntimeTypeGenerator
    {
        private readonly struct NameAndType
        {
            public readonly string Name;
            public readonly Type Type;

            public NameAndType(string name, Type type)
            {
                Name = name;
                Type = type;
            }
        }

        /// <summary>
        /// reflection 에서 기반 클래스가 없어서 함수로 제작
        /// </summary>
        /// <typeparam name="T">type/name을 가지고있는것 PropertyInfo나 ParameterInfo<</typeparam>
        /// <param name="typeBuilder">builder</param>
        /// <param name="props">프로퍼티를 만들 목록/param>
        /// <param name="fn">이름과 실제 타입을 반환</param>
        /// <return>생성된 field 정보</return>
        private static FieldBuilder[] DefineProperty<T>(TypeBuilder typeBuilder, IReadOnlyList<T> props, Func<T, NameAndType> fn)
        {
            const string memberPrefix = "m_";
            const string getterMethodPrefix = "get_";
            FieldBuilder[] fields = new FieldBuilder[props.Count];

            for (int i = 0; i < props.Count; ++i)
            {
                // int m_value;
                // int value { get; }
                // int get_value() { return this.m_value; } <- 이게 위에 프로퍼티에서 get; 

                NameAndType np = fn(props[i]);

                fields[i] = typeBuilder.DefineField(
                    memberPrefix + np.Name,
                    np.Type,
                    FieldAttributes.Public);

                PropertyBuilder property = typeBuilder.DefineProperty(
                    np.Name,
                    PropertyAttributes.HasDefault,
                    np.Type,
                    null);

                MethodBuilder propertyGetter = typeBuilder.DefineMethod(
                    getterMethodPrefix + np.Name,
                    MethodAttributes.Public,
                    np.Type,
                    null);

                ILGenerator gil = propertyGetter.GetILGenerator();
                gil.Emit(OpCodes.Ldarg_0);
                gil.Emit(OpCodes.Ldfld, fields[i]);
                gil.Emit(OpCodes.Ret);

                property.SetGetMethod(propertyGetter);
            }

            return fields;
        }

        /// <summary>
        ///  class Anonymous_generate_1 {
        ///     public int m_a;
        ///     public int m_b;
        ///     
        ///     public int a { get { return this.m_a } }
        ///     public int b { get { return this.m_b } }
        ///
        ///     public Anonymous_generate_1(Entity entity) {
        ///         this.m_a = entity.a;
        ///         this.m_b = entity.b;
        ///     }
        ///  }
        /// </summary>
        public static Type GenerateAnonymousEntityFromEntity(Type entityType)
        {
            IReadOnlyList<PropertyInfo> properties = entityType
                .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(p => p.GetCustomAttribute<NotMappedAttribute>() is null)
                .ToList();

            int gen = Interlocked.Increment(ref generateCount);

            TypeBuilder typeBuilder = moduleBuilder.DefineType("Anonymous_generate_e" + gen);
            FieldBuilder[] fields = DefineProperty(
                typeBuilder,
                properties,
                (p) => new NameAndType(p.Name, p.PropertyType));

            ConstructorBuilder ctor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                new Type[] { entityType });

            ILGenerator il = ctor.GetILGenerator();

            for (int i = 0; i < properties.Count; ++i)
            {
                MethodInfo getter = properties[i].GetGetMethod(nonPublic: true);
                // this.field[i] = arg[i]
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.EmitCall(OpCodes.Call, getter, null);
                il.Emit(OpCodes.Stfld, fields[i]);
            }

            il.Emit(OpCodes.Ret);

            ctor.DefineParameter(1, ParameterAttributes.In, "entity");

            return typeBuilder.CreateType();
        }

        /// <summary>
        ///  class Anonymous_generate_1 {
        ///     public int m_a;
        ///     public int m_b;
        ///     
        ///     public int a { get { return this.m_a } }
        ///     public int b { get { return this.m_b } }
        ///
        ///     public Anonymous_generate_1(int a, int b) {
        ///         this.m_a = a;
        ///         this.m_b = b;
        ///     }
        ///  }
        /// </summary>
        public static Type GenerateAnonymousEntityFromParameter(IReadOnlyList<ParameterInfo> parameters)
        {
            int gen = Interlocked.Increment(ref generateCount);

            TypeBuilder typeBuilder = moduleBuilder.DefineType("Anonymous_generate_p" + gen);
            FieldBuilder[] fields = DefineProperty(
                typeBuilder,
                parameters,
                p => new NameAndType(p.Name, p.ParameterType));

            ConstructorBuilder ctor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                parameters.Select(e => e.ParameterType).ToArray());


            ILGenerator il = ctor.GetILGenerator();

            for (int i = 0; i < parameters.Count; ++i)
            {
                // this.field[i] = arg[i]
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg, i + 1);
                il.Emit(OpCodes.Stfld, fields[i]);
            }

            il.Emit(OpCodes.Ret);

            for (int i = 0; i < parameters.Count; ++i)
            {
                ctor.DefineParameter(i + 1, ParameterAttributes.In, parameters[i].Name);
            }

            return typeBuilder.CreateType();
        }

    }
}
