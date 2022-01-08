using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;

namespace Dpa.Repository.Implements.Runtime
{
    public partial class RuntimeTypeGenerator
    {
        /// <summary>
        /// Func<T, object> fn_generate_1<T>() {
        ///     return (T entity) => {
        ///         return new Anonymous_generate_1(entity);
        ///     } 
        /// }
        /// </summary>
        public static Func<T, object> CreateFunctionClonePropertyAnonymousEntity<T>()
        {
            Type entityType = typeof(T);
            Type newType = GenerateAnonymousEntityFromEntity(entityType);
            int gen = Interlocked.Increment(ref generateCount);

            DynamicMethod m = new DynamicMethod("Fn_generate_" + gen, newType, new Type[] { entityType }, true);
            ConstructorInfo ctor = newType.GetConstructors()[0];

            var il = m.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);

            Func<T, object> fn = (Func<T, object>)m.CreateDelegate(typeof(Func<T, object>));
            return fn;
        }
    }
}
