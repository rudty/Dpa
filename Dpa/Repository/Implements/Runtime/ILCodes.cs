using Dpa.Repository.Implements.Types;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace Dpa.Repository.Implements.Runtime
{
    public static class ILCodes
    {
        private static readonly MethodInfo mMoveNext = typeof(System.Collections.IEnumerator).GetMethod("MoveNext");

        /// <summary>
        /// reflection 에서 기반 클래스가 없어서 함수로 제작
        /// </summary>
        /// <typeparam name="T">type/name을 가지고있는것 PropertyInfo나 ParameterInfo<</typeparam>
        /// <param name="typeBuilder">builder</param>
        /// <param name="props">프로퍼티를 만들 목록/param>
        /// <return>생성된 field 정보</return>
        internal static FieldBuilder[] DefineFieldAndProperty<T>(this TypeBuilder typeBuilder, EntityCollection<T> props)
        {
            const string memberPrefix = "m_";
            const string getterMethodPrefix = "get_";
            FieldBuilder[] fields = new FieldBuilder[props.Count];

            for (int i = 0; i < props.Count; ++i)
            {
                // int m_value;
                // int value { get; }
                // int get_value() { return this.m_value; } <- 이게 위에 프로퍼티에서 get; 

                string fieldName = props[i].MemberName;
                Type entityType = props[i].ColumnType;

                fields[i] = typeBuilder.DefineField(
                    memberPrefix + fieldName,
                    entityType,
                    FieldAttributes.Public);

                PropertyBuilder property = typeBuilder.DefineProperty(
                    fieldName,
                    PropertyAttributes.HasDefault,
                    entityType,
                    null);

                MethodBuilder propertyGetter = typeBuilder.DefineMethod(
                    getterMethodPrefix + fieldName,
                    MethodAttributes.Public,
                    entityType,
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
        /// ForEachLoop  아래 코드와 비슷한 코드를 작성합니다
        /// 
        /// 시작시 처음 IEnumerable iter는 스택에 담겨 있어야 합니다
        /// 
        /// IEnumerator<T> it = iter.GetEnumerator();
        /// lBeginLoop:
        /// if (false == it.MoveNext()) goto lEndLoop;
        /// onLoop(it.Current);
        /// goto lBeginLoop;
        /// lEndLoop:
        /// </summary>
        /// <param name="il">il</param>
        /// <param name="elemType">IEnumerable의 타입 T</param>
        /// <param name="onLoop">루프 안에서 동작, 저장된 변수 전달</param>
        public static void ForEachInline(ILGenerator il, Type elemType, Action<LocalBuilder> onLoop)
        {
            Type enumerbleType = typeof(IEnumerable<>).MakeGenericType(elemType);

            MethodInfo mGetEnumerator = enumerbleType.GetMethod("GetEnumerator");
            MethodInfo mCurrent = mGetEnumerator.ReturnType
                .GetProperty("Current")
                .GetGetMethod();

            LocalBuilder iter = il.DeclareLocal(mGetEnumerator.ReturnType);
            LocalBuilder current = il.DeclareLocal(elemType);
            Label lBeginLoop = il.DefineLabel();
            Label lEndLoop = il.DefineLabel();

            il.Emit(OpCodes.Callvirt, mGetEnumerator);
            il.Emit(OpCodes.Stloc, iter);

            il.MarkLabel(lBeginLoop);

            il.Emit(OpCodes.Ldloc, iter);
            il.Emit(OpCodes.Callvirt, mMoveNext);
            il.Emit(OpCodes.Brfalse_S, lEndLoop);

            il.Emit(OpCodes.Ldloc, iter);
            il.Emit(OpCodes.Callvirt, mCurrent);
            il.Emit(OpCodes.Stloc, current);
            onLoop(current);

            il.Emit(OpCodes.Br_S, lBeginLoop); // goto lBeginLoop

            il.MarkLabel(lEndLoop);
        }

        /// <summary>
        /// new int[] { 1,2,3,4 };
        /// 배열을 생성하고 초기화는 코드를 작성합니다
        /// </summary>
        /// <param name="il">il</param>
        /// <param name="elemType">배열의 원소 타입</param>
        /// <param name="size">크기</param>
        /// <param name="onAssignAt">i번째 요소에 할당할 원소</param>
        public static void NewArrayAndAssignInline(ILGenerator il, Type elemType, int size, Action<int> onAssignAt)
        {
            il.Emit(OpCodes.Ldc_I4, size);
            il.Emit(OpCodes.Newarr, elemType);

            for (int i = 0; i < size; ++i)
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4, i);
                onAssignAt(i);
                il.Emit(OpCodes.Stelem_Ref);
            }
        }
    }
}
