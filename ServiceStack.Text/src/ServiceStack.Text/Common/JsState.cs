using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ServiceStack.Text.Common
{
    internal static class JsState
    {
        //Exposing field for perf
        [ThreadStatic]
        internal static int WritingKeyCount = 0;

        [ThreadStatic]
        internal static bool IsWritingValue = false;

        [ThreadStatic]
        internal static bool IsWritingDynamic = false;

        [ThreadStatic]
        internal static bool IsRuntimeType = false;

        [ThreadStatic]
        internal static bool QueryStringMode = false;

        [ThreadStatic]
        internal static int Depth = 0;

        [ThreadStatic]
        internal static bool IsCsv = false;


        [ThreadStatic]
        internal static HashSet<Type> InSerializerFns = new HashSet<Type>();

        internal static void RegisterSerializer<T>()
        {
            if (InSerializerFns == null)
                InSerializerFns = new HashSet<Type>();

            InSerializerFns.Add(typeof(T));
        }

        internal static void UnRegisterSerializer<T>()
        {
            if (InSerializerFns == null)
                return;

            InSerializerFns.Remove(typeof(T));
        }

        internal static bool InSerializer<T>()
        {
            return InSerializerFns != null && InSerializerFns.Contains(typeof(T));
        }

        [ThreadStatic]
        internal static HashSet<Type> InDeserializerFns;

        internal static void RegisterDeserializer<T>()
        {
            if (InDeserializerFns == null)
                InDeserializerFns = new HashSet<Type>();

            InDeserializerFns.Add(typeof(T));
        }

        internal static void UnRegisterDeserializer<T>()
        {
            if (InDeserializerFns == null)
                return;

            InDeserializerFns.Remove(typeof(T));
        }

        internal static bool InDeserializer<T>()
        {
            return InDeserializerFns != null && InDeserializerFns.Contains(typeof(T));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Traverse(object value)
        {
            if (++Depth <= JsConfig.MaxDepth) 
                return true;
            
            Tracer.Instance.WriteError(
                $"Exceeded MaxDepth limit of {JsConfig.MaxDepth} attempting to serialize {value.GetType().Name}");
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void UnTraverse() => --Depth;

        internal static void Reset()
        {
            InSerializerFns = null;
            InDeserializerFns = null;
        }
    }
}