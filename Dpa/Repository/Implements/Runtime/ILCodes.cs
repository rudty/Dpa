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
    }
}
