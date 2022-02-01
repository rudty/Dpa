using Dpa.Repository.Implements.Types;
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
        internal static Type GenerateAnonymousEntityFromEntity(Type entityType, Func<Column<PropertyInfo>, bool> selector = null)
        {
            Entity<PropertyInfo> properties = entityType.GetMappingProperties(selector);
            int gen = Interlocked.Increment(ref generateCount);

            TypeBuilder typeBuilder = moduleBuilder.DefineType("Anonymous_generate_e" + gen);
            FieldBuilder[] fields = typeBuilder.DefineFieldAndProperty(properties);

            ConstructorBuilder ctor = typeBuilder.DefineConstructor(
                         MethodAttributes.Public,
                         CallingConventions.Standard,
                         new Type[] { entityType });

            ILGenerator il = ctor.GetILGenerator();

            OpCode loadThis = OpCodes.Ldarg;
            if (entityType.IsValueType)
            {
                loadThis = OpCodes.Ldarga;
            }

            for (int i = 0; i < properties.Count; ++i)
            {
                MethodInfo getter = properties[i].Info.GetGetMethod(nonPublic: true);
                // this.field[i] = arg[i]
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(loadThis, 1);
                il.EmitCall(OpCodes.Call, getter, null);
                if (properties[i].MemberToColumn != null)
                {
                    properties[i].MemberToColumn(il);    
                }
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
        internal static Type GenerateAnonymousEntityFromParameter(Entity<ParameterInfo> parameters)
        {
            int gen = Interlocked.Increment(ref generateCount);

            TypeBuilder typeBuilder = moduleBuilder.DefineType("Anonymous_generate_p" + gen);
            FieldBuilder[] fields = typeBuilder.DefineFieldAndProperty(parameters);

            ConstructorBuilder ctor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                fields.Select(e => e.FieldType).ToArray());

            ILGenerator il = ctor.GetILGenerator();

            for (int i = 0; i < parameters.Count; ++i)
            {
                // this.field[i] = arg[i]
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg, i + 1);
                if (parameters[i].MemberToColumn != null)
                {
                    parameters[i].MemberToColumn(il);
                }

                il.Emit(OpCodes.Stfld, fields[i]);

                ctor.DefineParameter(i + 1, ParameterAttributes.In, parameters[i].ColumnName);
            }

            il.Emit(OpCodes.Ret);

            return typeBuilder.CreateType();
        }

    }
}
