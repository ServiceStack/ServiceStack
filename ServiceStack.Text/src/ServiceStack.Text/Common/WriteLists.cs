//
// https://github.com/ServiceStack/ServiceStack.Text
// ServiceStack.Text: .NET C# POCO JSON, JSV and CSV Text Serializers.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2012 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Linq;

namespace ServiceStack.Text.Common
{
    public static class WriteListsOfElements<TSerializer>
        where TSerializer : ITypeSerializer
    {
        private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

        static Dictionary<Type, WriteObjectDelegate> ListCacheFns = new Dictionary<Type, WriteObjectDelegate>();

        public static WriteObjectDelegate GetListWriteFn(Type elementType)
        {
            WriteObjectDelegate writeFn;
            if (ListCacheFns.TryGetValue(elementType, out writeFn)) return writeFn;

            var genericType = typeof(WriteListsOfElements<,>).MakeGenericType(elementType, typeof(TSerializer));
            var mi = genericType.GetStaticMethod("WriteList");
            writeFn = (WriteObjectDelegate)mi.MakeDelegate(typeof(WriteObjectDelegate));

            Dictionary<Type, WriteObjectDelegate> snapshot, newCache;
            do
            {
                snapshot = ListCacheFns;
                newCache = new Dictionary<Type, WriteObjectDelegate>(ListCacheFns);
                newCache[elementType] = writeFn;

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref ListCacheFns, newCache, snapshot), snapshot));

            return writeFn;
        }


        static Dictionary<Type, WriteObjectDelegate> IListCacheFns = new Dictionary<Type, WriteObjectDelegate>();

        public static WriteObjectDelegate GetIListWriteFn(Type elementType)
        {
            WriteObjectDelegate writeFn;
            if (IListCacheFns.TryGetValue(elementType, out writeFn)) return writeFn;

            var genericType = typeof(WriteListsOfElements<,>).MakeGenericType(elementType, typeof(TSerializer));
            var mi = genericType.GetStaticMethod("WriteIList");
            writeFn = (WriteObjectDelegate)mi.MakeDelegate(typeof(WriteObjectDelegate));

            Dictionary<Type, WriteObjectDelegate> snapshot, newCache;
            do
            {
                snapshot = IListCacheFns;
                newCache = new Dictionary<Type, WriteObjectDelegate>(IListCacheFns);
                newCache[elementType] = writeFn;

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref IListCacheFns, newCache, snapshot), snapshot));

            return writeFn;
        }

        static Dictionary<Type, WriteObjectDelegate> CacheFns = new Dictionary<Type, WriteObjectDelegate>();

        public static WriteObjectDelegate GetGenericWriteArray(Type elementType)
        {
            WriteObjectDelegate writeFn;
            if (CacheFns.TryGetValue(elementType, out writeFn)) return writeFn;

            var genericType = typeof(WriteListsOfElements<,>).MakeGenericType(elementType, typeof(TSerializer));
            var mi = genericType.GetStaticMethod("WriteArray");
            writeFn = (WriteObjectDelegate)mi.MakeDelegate(typeof(WriteObjectDelegate));

            Dictionary<Type, WriteObjectDelegate> snapshot, newCache;
            do
            {
                snapshot = CacheFns;
                newCache = new Dictionary<Type, WriteObjectDelegate>(CacheFns);
                newCache[elementType] = writeFn;

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref CacheFns, newCache, snapshot), snapshot));

            return writeFn;
        }

        static Dictionary<Type, WriteObjectDelegate> EnumerableCacheFns = new Dictionary<Type, WriteObjectDelegate>();

        public static WriteObjectDelegate GetGenericWriteEnumerable(Type elementType)
        {
            WriteObjectDelegate writeFn;
            if (EnumerableCacheFns.TryGetValue(elementType, out writeFn)) return writeFn;

            var genericType = typeof(WriteListsOfElements<,>).MakeGenericType(elementType, typeof(TSerializer));
            var mi = genericType.GetStaticMethod("WriteEnumerable");
            writeFn = (WriteObjectDelegate)mi.MakeDelegate(typeof(WriteObjectDelegate));

            Dictionary<Type, WriteObjectDelegate> snapshot, newCache;
            do
            {
                snapshot = EnumerableCacheFns;
                newCache = new Dictionary<Type, WriteObjectDelegate>(EnumerableCacheFns);
                newCache[elementType] = writeFn;

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref EnumerableCacheFns, newCache, snapshot), snapshot));

            return writeFn;
        }

        static Dictionary<Type, WriteObjectDelegate> ListValueTypeCacheFns = new Dictionary<Type, WriteObjectDelegate>();

        public static WriteObjectDelegate GetWriteListValueType(Type elementType)
        {
            WriteObjectDelegate writeFn;
            if (ListValueTypeCacheFns.TryGetValue(elementType, out writeFn)) return writeFn;

            var genericType = typeof(WriteListsOfElements<,>).MakeGenericType(elementType, typeof(TSerializer));
            var mi = genericType.GetStaticMethod("WriteListValueType");
            writeFn = (WriteObjectDelegate)mi.MakeDelegate(typeof(WriteObjectDelegate));

            Dictionary<Type, WriteObjectDelegate> snapshot, newCache;
            do
            {
                snapshot = ListValueTypeCacheFns;
                newCache = new Dictionary<Type, WriteObjectDelegate>(ListValueTypeCacheFns);
                newCache[elementType] = writeFn;

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref ListValueTypeCacheFns, newCache, snapshot), snapshot));

            return writeFn;
        }

        static Dictionary<Type, WriteObjectDelegate> IListValueTypeCacheFns = new Dictionary<Type, WriteObjectDelegate>();

        public static WriteObjectDelegate GetWriteIListValueType(Type elementType)
        {
            WriteObjectDelegate writeFn;

            if (IListValueTypeCacheFns.TryGetValue(elementType, out writeFn)) return writeFn;

            var genericType = typeof(WriteListsOfElements<,>).MakeGenericType(elementType, typeof(TSerializer));
            var mi = genericType.GetStaticMethod("WriteIListValueType");
            writeFn = (WriteObjectDelegate)mi.MakeDelegate(typeof(WriteObjectDelegate));

            Dictionary<Type, WriteObjectDelegate> snapshot, newCache;
            do
            {
                snapshot = IListValueTypeCacheFns;
                newCache = new Dictionary<Type, WriteObjectDelegate>(IListValueTypeCacheFns);
                newCache[elementType] = writeFn;

            } while (!ReferenceEquals(
                Interlocked.CompareExchange(ref IListValueTypeCacheFns, newCache, snapshot), snapshot));

            return writeFn;
        }

        public static void WriteIEnumerable(TextWriter writer, object oValueCollection)
        {
            WriteObjectDelegate toStringFn = null;

            writer.Write(JsWriter.ListStartChar);

            var valueCollection = (IEnumerable)oValueCollection;
            var ranOnce = false;
            Type lastType = null;
            foreach (var valueItem in valueCollection)
            {
                if ((toStringFn == null) || (valueItem != null && valueItem.GetType() != lastType))
                {
                    if (valueItem != null)
                    {
                        if (valueItem.GetType() != lastType)
                        {
                            lastType = valueItem.GetType();
                            toStringFn = Serializer.GetWriteFn(lastType);
                        }
                    }
                    else
                    {
                        // this can happen if the first item in the collection was null
                        lastType = typeof(object);
                        toStringFn = Serializer.GetWriteFn(lastType);
                    }
                }

                JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);

                toStringFn(writer, valueItem);
            }

            writer.Write(JsWriter.ListEndChar);
        }
    }

    public static class WriteListsOfElements<T, TSerializer>
        where TSerializer : ITypeSerializer
    {
        private static readonly WriteObjectDelegate ElementWriteFn;

        static WriteListsOfElements()
        {
            var fn = JsWriter.GetTypeSerializer<TSerializer>().GetWriteFn<T>();
            ElementWriteFn = (writer, obj) => {
                try 
                { 
                    if (!JsState.Traverse(obj))
                        return;
                    
                    fn(writer, obj);
                }
                finally 
                {
                    JsState.UnTraverse();
                }
            };
        }

        public static void WriteList(TextWriter writer, object oList)
        {
            WriteGenericIList(writer, (IList<T>)oList);
        }

        public static void WriteGenericList(TextWriter writer, List<T> list)
        {
            writer.Write(JsWriter.ListStartChar);

            var ranOnce = false;
            var listLength = list.Count;
            for (var i = 0; i < listLength; i++)
            {
                JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
                ElementWriteFn(writer, list[i]);
            }

            writer.Write(JsWriter.ListEndChar);
        }

        public static void WriteListValueType(TextWriter writer, object list)
        {
            WriteGenericListValueType(writer, (List<T>)list);
        }

        public static void WriteGenericListValueType(TextWriter writer, List<T> list)
        {
            if (list == null) return; //AOT

            writer.Write(JsWriter.ListStartChar);

            var ranOnce = false;
            var listLength = list.Count;
            for (var i = 0; i < listLength; i++)
            {
                JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
                ElementWriteFn(writer, list[i]);
            }

            writer.Write(JsWriter.ListEndChar);
        }

        public static void WriteIList(TextWriter writer, object oList)
        {
            WriteGenericIList(writer, (IList<T>)oList);
        }

        public static void WriteGenericIList(TextWriter writer, IList<T> list)
        {
            if (list == null) return;
            writer.Write(JsWriter.ListStartChar);

            var ranOnce = false;
            var listLength = list.Count;
            try
            {
                for (var i = 0; i < listLength; i++)
                {
                    JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
                    ElementWriteFn(writer, list[i]);
                }

            }
            catch (Exception ex)
            {
                Tracer.Instance.WriteError(ex);
                throw;
            }
            writer.Write(JsWriter.ListEndChar);
        }

        public static void WriteIListValueType(TextWriter writer, object list)
        {
            WriteGenericIListValueType(writer, (IList<T>)list);
        }

        public static void WriteGenericIListValueType(TextWriter writer, IList<T> list)
        {
            if (list == null) return; //AOT

            writer.Write(JsWriter.ListStartChar);

            var ranOnce = false;
            var listLength = list.Count;
            for (var i = 0; i < listLength; i++)
            {
                JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
                ElementWriteFn(writer, list[i]);
            }

            writer.Write(JsWriter.ListEndChar);
        }

        public static void WriteArray(TextWriter writer, object oArrayValue)
        {
            if (oArrayValue == null) return;
            WriteGenericArray(writer, (Array)oArrayValue);
        }

        public static void WriteGenericArrayValueType(TextWriter writer, object oArray)
        {
            WriteGenericArrayValueType(writer, (T[])oArray);
        }

        public static void WriteGenericArrayValueType(TextWriter writer, T[] array)
        {
            if (array == null) return;
            writer.Write(JsWriter.ListStartChar);

            var ranOnce = false;
            var arrayLength = array.Length;
            for (var i = 0; i < arrayLength; i++)
            {
                JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
                ElementWriteFn(writer, array[i]);
            }

            writer.Write(JsWriter.ListEndChar);
        }

        private static void WriteGenericArrayMultiDimension(TextWriter writer, Array array, int rank, int[] indices)
        {
            var ranOnce = false;
            writer.Write(JsWriter.ListStartChar);
            for (int i = 0; i < array.GetLength(rank); i++)
            {
                JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
                indices[rank] = i;

                if (rank < (array.Rank - 1))
                    WriteGenericArrayMultiDimension(writer, array, rank + 1, indices);
                else
                    ElementWriteFn(writer, array.GetValue(indices));
            }
            writer.Write(JsWriter.ListEndChar);
        }

        public static void WriteGenericArray(TextWriter writer, Array array)
        {
            WriteGenericArrayMultiDimension(writer, array, 0, new int[array.Rank]);
        }
        public static void WriteEnumerable(TextWriter writer, object oEnumerable)
        {
            WriteGenericEnumerable(writer, (IEnumerable<T>)oEnumerable);
        }

        public static void WriteGenericEnumerable(TextWriter writer, IEnumerable<T> enumerable)
        {
            if (enumerable == null) return;
            writer.Write(JsWriter.ListStartChar);

            var ranOnce = false;
            foreach (var value in enumerable)
            {
                JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
                ElementWriteFn(writer, value);
            }

            writer.Write(JsWriter.ListEndChar);
        }

        public static void WriteGenericEnumerableValueType(TextWriter writer, IEnumerable<T> enumerable)
        {
            writer.Write(JsWriter.ListStartChar);

            var ranOnce = false;
            foreach (var value in enumerable)
            {
                JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
                ElementWriteFn(writer, value);
            }

            writer.Write(JsWriter.ListEndChar);
        }
    }

    internal static class WriteLists
    {
        public static void WriteListString(ITypeSerializer serializer, TextWriter writer, object list)
        {
            WriteListString(serializer, writer, (List<string>)list);
        }

        public static void WriteListString(ITypeSerializer serializer, TextWriter writer, List<string> list)
        {
            writer.Write(JsWriter.ListStartChar);

            var ranOnce = false;
            foreach (var x in list)
            {
                JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
                serializer.WriteString(writer, x);
            }

            writer.Write(JsWriter.ListEndChar);
        }

        public static void WriteIListString(ITypeSerializer serializer, TextWriter writer, object list)
        {
            WriteIListString(serializer, writer, (IList<string>)list);
        }

        public static void WriteIListString(ITypeSerializer serializer, TextWriter writer, IList<string> list)
        {
            writer.Write(JsWriter.ListStartChar);

            var ranOnce = false;
            var listLength = list.Count;
            for (var i = 0; i < listLength; i++)
            {
                JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
                serializer.WriteString(writer, list[i]);
            }

            writer.Write(JsWriter.ListEndChar);
        }

        public static void WriteBytes(ITypeSerializer serializer, TextWriter writer, object byteValue)
        {
            if (byteValue == null) return;
            serializer.WriteBytes(writer, byteValue);
        }

        public static void WriteStringArray(ITypeSerializer serializer, TextWriter writer, object oList)
        {
            writer.Write(JsWriter.ListStartChar);

            var list = (string[])oList;
            var ranOnce = false;
            var listLength = list.Length;
            for (var i = 0; i < listLength; i++)
            {
                JsWriter.WriteItemSeperatorIfRanOnce(writer, ref ranOnce);
                serializer.WriteString(writer, list[i]);
            }

            writer.Write(JsWriter.ListEndChar);
        }
    }

    internal static class WriteLists<T, TSerializer>
        where TSerializer : ITypeSerializer
    {
        private static readonly WriteObjectDelegate CacheFn;
        private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

        static WriteLists()
        {
            CacheFn = GetWriteFn();
        }

        public static WriteObjectDelegate Write
        {
            get { return CacheFn; }
        }

        public static WriteObjectDelegate GetWriteFn()
        {
            var type = typeof(T);

            var listInterface = type.GetTypeWithGenericTypeDefinitionOf(typeof(IList<>));
            if (listInterface == null)
                throw new ArgumentException(string.Format("Type {0} is not of type IList<>", type.FullName));

            //optimized access for regularly used types
            if (type == typeof(List<string>))
                return (w, x) => WriteLists.WriteListString(Serializer, w, x);
            if (type == typeof(IList<string>))
                return (w, x) => WriteLists.WriteIListString(Serializer, w, x);

            if (type == typeof(List<int>))
                return WriteListsOfElements<int, TSerializer>.WriteListValueType;
            if (type == typeof(IList<int>))
                return WriteListsOfElements<int, TSerializer>.WriteIListValueType;

            if (type == typeof(List<long>))
                return WriteListsOfElements<long, TSerializer>.WriteListValueType;
            if (type == typeof(IList<long>))
                return WriteListsOfElements<long, TSerializer>.WriteIListValueType;

            var elementType = listInterface.GetGenericArguments()[0];

            var isGenericList = typeof(T).IsGenericType
                && typeof(T).GetGenericTypeDefinition() == typeof(List<>);

            if (elementType.IsValueType
                && JsWriter.ShouldUseDefaultToStringMethod(elementType))
            {
                if (isGenericList)
                    return WriteListsOfElements<TSerializer>.GetWriteListValueType(elementType);

                return WriteListsOfElements<TSerializer>.GetWriteIListValueType(elementType);
            }

            return isGenericList
                ? WriteListsOfElements<TSerializer>.GetListWriteFn(elementType)
                : WriteListsOfElements<TSerializer>.GetIListWriteFn(elementType);
        }

    }
}