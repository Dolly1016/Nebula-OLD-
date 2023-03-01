using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Il2CppInterop.Runtime.Runtime;
using Mono.Cecil;

using FieldInfo = Il2CppSystem.Reflection.FieldInfo;
using Object = Il2CppSystem.Object;
namespace Nebula
{
    public static class Reflection
    {
        private static T[] InsertIl2CppDef<T>(this T[] array, T first, T method, T returnType)
        {
            T[] newArray = new T[array.Length + 3];
            Array.Copy(array, 0, newArray, 1, array.Length);
            newArray[0] = first;
            newArray[^2] = method;
            newArray[^1] = returnType;
            return newArray;
        }

        private static T[] InsertIl2CppArgs<T>(this T[] array, T first, T last)
        {
            T[] newArray = new T[array.Length + 2];
            Array.Copy(array, 0, newArray, 1, array.Length);
            newArray[0] = first;
            newArray[^1] = last;
            return newArray;
        }

        private static MethodInfo m_getDelegate;
        private static MethodInfo getDelegate
        {
            get
            {
                if (m_getDelegate == null)
                    m_getDelegate = typeof(Marshal).GetMethod("GetDelegateForFunctionPointerInternal", AccessTools.all);
                return m_getDelegate;
            }
        }

        public unsafe static object CallBase<T, TDelegate>(this Object obj, string name, params object[] args)
        where T : Object
        where TDelegate : Delegate
        {
            Type delegateType = typeof(TDelegate);
            MethodInfo method = delegateType.GetMethod("Invoke");
            Type returnType = method.ReturnType;
            Type[] paramTypes = method.GetParameters().Select(info => info.ParameterType).ToArray();
            string[] parameters = paramTypes.Select(info => info.FullName).ToArray();

            for (int i = 0; i < paramTypes.Length; i++)
            {
                if ((typeof(Il2CppObjectBase)).IsAssignableFrom(paramTypes[i]))
                {
                    args[i] = IL2CPP.Il2CppObjectBaseToPtr((Il2CppObjectBase)args[i]);
                    paramTypes[i] = typeof(IntPtr);
                }
            }

            IntPtr klass = Il2CppClassPointerStore<T>.NativeClassPtr;
            IntPtr methodPtr = IL2CPP.GetIl2CppMethod(klass, false, name, returnType.FullName, parameters);
            Type il2cppDelegateType = Expression.GetDelegateType(paramTypes.InsertIl2CppDef(typeof(IntPtr), typeof(Il2CppMethodInfo*), returnType));

            Delegate funcDelegate = (Delegate)getDelegate.Invoke(null, new object[] { *(IntPtr*)methodPtr, il2cppDelegateType });
            return funcDelegate.DynamicInvoke(args.InsertIl2CppArgs(obj.Pointer, IntPtr.Zero));
        }

        private unsafe delegate void BaseFunc(IntPtr arg1, Il2CppMethodInfo* arg2);
        public unsafe static void CallBase<T>(this Object obj, string name)
            where T : Object
        {
            IntPtr klass = Il2CppClassPointerStore<T>.NativeClassPtr;
            IntPtr methodPtr = IL2CPP.GetIl2CppMethod(klass, false, name, "System.Void", Array.Empty<string>());
            BaseFunc baseFunc = Marshal.GetDelegateForFunctionPointer<BaseFunc>(*(IntPtr*)methodPtr);
            baseFunc?.Invoke(obj.Pointer, (Il2CppMethodInfo*)IntPtr.Zero);
        }
    }
}
