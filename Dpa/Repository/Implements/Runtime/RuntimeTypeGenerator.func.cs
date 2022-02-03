using Dpa.Repository.Implements.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
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
        internal static Func<ID, object> CreateFunctionPrimaryKeyAnonymousEntity<T, ID>()
        {
            Entity<PropertyInfo> entity = typeof(T).GetMappingProperties();
            Type newType = GenerateAnonymousEntityFromParameter(new Entity<PropertyInfo>(entity.GetPrimaryKeys()));
            int gen = Interlocked.Increment(ref generateCount);

            DynamicMethod m = new DynamicMethod("Fn_clone_propertygenerate_" + gen, newType, new Type[] { typeof(ID) }, true);
            ConstructorInfo ctor = newType.GetConstructors()[0];

            var il = m.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);

            Func<ID, object> fn = (Func<ID, object>)m.CreateDelegate(typeof(Func<ID, object>));
            return fn;
        }

        /// <summary>
        /// Func<T, object> fn_generate_1<T>() {
        ///     return (T entity) => {
        ///         return new Anonymous_generate_1(entity);
        ///     } 
        /// }
        /// </summary>
        internal static Func<T, object> CreateFunctionClonePropertyAnonymousEntity<T>(Func<Column<PropertyInfo>, bool> selector = null)
        {
            Type entityType = typeof(T);
            Type newType = GenerateAnonymousEntityFromEntity(entityType, selector);
            int gen = Interlocked.Increment(ref generateCount);

            DynamicMethod m = new DynamicMethod("Fn_clone_propertygenerate_" + gen, newType, new Type[] { entityType }, true);
            ConstructorInfo ctor = newType.GetConstructors()[0];

            var il = m.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Ret);

            Func<T, object> fn = (Func<T, object>)m.CreateDelegate(typeof(Func<T, object>));
            return fn;
        }

        private static Type dataTableType = typeof(DataTable);
        private static Type columnsCollectionType = typeof(DataColumnCollection);
        private static Type rowsCollectionType = typeof(DataRowCollection);
        private static ConstructorInfo dataTableEmptyCtor = dataTableType.GetConstructor(Type.EmptyTypes);
        private static PropertyInfo columnsProperty = dataTableType.GetProperty("Columns");
        private static PropertyInfo rowsProperty = dataTableType.GetProperty("Rows");
        private static MethodInfo columnGetter = columnsProperty.GetGetMethod(nonPublic: true);
        private static MethodInfo rowsGetter = rowsProperty.GetGetMethod(nonPublic: true);
        private static MethodInfo columnAddMethod = columnsCollectionType.GetMethod("Add", new Type[] { typeof(string) });
        private static MethodInfo rowAddMethod = rowsCollectionType.GetMethod("Add", new Type[] { typeof(object[]) });

        /// <summary>
        /// entity를 datatable로 변경합니다
        /// class Hello {
        ///     int a { get; }
        ///     int b { get; }
        /// }
        /// 이고
        /// Hello[] helloArray .... 면
        /// 
        /// DataTable d = new DataTable();
        /// d.Columns.Add("a");
        /// d.Columns.Add("b");
        /// foreach (Hello hello in helloArray) {
        ///     d.Rows.Add(hello.a, hello.b);
        /// }
        /// 를 작성합니다
        /// 
        /// 이때 컬렉션 요소는 반드시 스택에 있어야합니다
        /// </summary>
        /// <param name="il">il</param>
        /// <param name="enumerableType">typeof(T[])</param>
        public static void CreateDataTableParameterFromEntityInline(ILGenerator il, Type enumerableType)
        {
            Type entityType = enumerableType.GetGenericArguments()[0];

            int gen = Interlocked.Increment(ref generateCount);
            LocalBuilder fDataTable = il.DeclareLocal(dataTableType);

            //// DataTable dataTable = new DataTable();
            il.Emit(OpCodes.Newobj, dataTableEmptyCtor);
            il.Emit(OpCodes.Stloc, fDataTable);

            // DataColumnCollection = dataTable.Columns;
            LocalBuilder fColumns = il.DeclareLocal(columnsCollectionType);

            il.Emit(OpCodes.Ldloc, fDataTable);
            il.Emit(OpCodes.Call, columnGetter);
            il.Emit(OpCodes.Stloc, fColumns);

            // DataRowCollection = dataTable.Rows;
            LocalBuilder fRows = il.DeclareLocal(rowsCollectionType);

            il.Emit(OpCodes.Ldloc, fDataTable);
            il.Emit(OpCodes.Call, rowsGetter);
            il.Emit(OpCodes.Stloc, fRows);

            Entity<PropertyInfo> props = entityType.GetMappingProperties();

            foreach (Column<PropertyInfo> p in props)
            {
                il.Emit(OpCodes.Ldloc, fColumns);
                il.Emit(OpCodes.Ldstr, p.ColumnName);
                il.Emit(OpCodes.Call, columnAddMethod);
                il.Emit(OpCodes.Pop);
            }

            //il.Emit(OpCodes.Ldarg_0);
            OpCode loadLocal = OpCodes.Ldloc;
            if (entityType.IsValueType)
            {
                loadLocal = OpCodes.Ldloca;
            }

            //  foreach(var e in list) {
            //      DataTable.Rows.Add(new object[] { ... });
            //  }
            ILCodes.ForEachInline(il, entityType, (LocalBuilder it) =>
            { 
                il.Emit(OpCodes.Ldloc, fRows);

                ILCodes.NewRefArrayAndAssignInline(il, typeof(object), props.Count, (int i) =>
                {
                    MethodInfo getter = props[i].Info.GetGetMethod(nonPublic: true);
                    il.Emit(loadLocal, it);
                    il.Emit(OpCodes.Call, getter);
                    if (getter.ReturnType.IsValueType)
                    {
                        il.Emit(OpCodes.Box, getter.ReturnType);
                    }
                });

                il.Emit(OpCodes.Call, rowAddMethod);
                il.Emit(OpCodes.Pop);
            });

            il.Emit(OpCodes.Ldloc, fDataTable);
            il.Emit(OpCodes.Call, DapperMethod.dapperAsTableValuedParameter);
        }
    }
}
