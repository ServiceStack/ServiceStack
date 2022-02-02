using System;
using System.Collections;
using System.Collections.Generic;

namespace ServiceStack.Text.Common
{
    internal static class DeserializeSpecializedCollections<T, TSerializer>
        where TSerializer : ITypeSerializer
    {
        private static readonly ParseStringSpanDelegate CacheFn;

        static DeserializeSpecializedCollections()
        {
            CacheFn = GetParseStringSpanFn();
        }

        public static ParseStringDelegate Parse => v => CacheFn(v.AsSpan());

        public static ParseStringSpanDelegate ParseStringSpan => CacheFn;

        public static ParseStringDelegate GetParseFn() => v => GetParseStringSpanFn()(v.AsSpan());

        public static ParseStringSpanDelegate GetParseStringSpanFn()
        {
            if (typeof(T).HasAnyTypeDefinitionsOf(typeof(Queue<>)))
            {
                if (typeof(T) == typeof(Queue<string>))
                    return ParseStringQueue;

                if (typeof(T) == typeof(Queue<int>))
                    return ParseIntQueue;

                return GetGenericQueueParseFn();
            }

            if (typeof(T).HasAnyTypeDefinitionsOf(typeof(Stack<>)))
            {
                if (typeof(T) == typeof(Stack<string>))
                    return ParseStringStack;

                if (typeof(T) == typeof(Stack<int>))
                    return ParseIntStack;

                return GetGenericStackParseFn();
            }

            var fn = PclExport.Instance.GetSpecializedCollectionParseStringSpanMethod<TSerializer>(typeof(T));
            if (fn != null)
                return fn;

            if (typeof(T) == typeof(IEnumerable) || typeof(T) == typeof(ICollection))
            {
                return GetEnumerableParseStringSpanFn();
            }

            return GetGenericEnumerableParseStringSpanFn();
        }

        public static Queue<string> ParseStringQueue(string value) => ParseStringQueue(value.AsSpan());

        public static Queue<string> ParseStringQueue(ReadOnlySpan<char> value)
        {
            var parse = (IEnumerable<string>)DeserializeList<List<string>, TSerializer>.ParseStringSpan(value);
            return new Queue<string>(parse);
        }

        public static Queue<int> ParseIntQueue(string value) => ParseIntQueue(value.AsSpan());


        public static Queue<int> ParseIntQueue(ReadOnlySpan<char> value)
        {
            var parse = (IEnumerable<int>)DeserializeList<List<int>, TSerializer>.ParseStringSpan(value);
            return new Queue<int>(parse);
        }

        internal static ParseStringSpanDelegate GetGenericQueueParseFn()
        {
            var enumerableInterface = typeof(T).GetTypeWithGenericInterfaceOf(typeof(IEnumerable<>));
            var elementType = enumerableInterface.GetGenericArguments()[0];
            var genericType = typeof(SpecializedQueueElements<>).MakeGenericType(elementType);
            var mi = genericType.GetStaticMethod("ConvertToQueue");
            var convertToQueue = (ConvertObjectDelegate)mi.MakeDelegate(typeof(ConvertObjectDelegate));

            var parseFn = DeserializeEnumerable<T, TSerializer>.GetParseStringSpanFn();

            return x => convertToQueue(parseFn(x));
        }

        public static Stack<string> ParseStringStack(string value) => ParseStringStack(value.AsSpan());

        public static Stack<string> ParseStringStack(ReadOnlySpan<char> value)
        {
            var parse = (IEnumerable<string>)DeserializeList<List<string>, TSerializer>.ParseStringSpan(value);
            return new Stack<string>(parse);
        }

        public static Stack<int> ParseIntStack(string value) => ParseIntStack(value.AsSpan());

        public static Stack<int> ParseIntStack(ReadOnlySpan<char> value)
        {
            var parse = (IEnumerable<int>)DeserializeList<List<int>, TSerializer>.ParseStringSpan(value);
            return new Stack<int>(parse);
        }

        internal static ParseStringSpanDelegate GetGenericStackParseFn()
        {
            var enumerableInterface = typeof(T).GetTypeWithGenericInterfaceOf(typeof(IEnumerable<>));

            var elementType = enumerableInterface.GetGenericArguments()[0];
            var genericType = typeof(SpecializedQueueElements<>).MakeGenericType(elementType);
            var mi = genericType.GetStaticMethod("ConvertToStack");
            var convertToQueue = (ConvertObjectDelegate)mi.MakeDelegate(typeof(ConvertObjectDelegate));

            var parseFn = DeserializeEnumerable<T, TSerializer>.GetParseStringSpanFn();

            return x => convertToQueue(parseFn(x));
        }

        public static ParseStringDelegate GetEnumerableParseFn() => DeserializeListWithElements<TSerializer>.ParseStringList;

        public static ParseStringSpanDelegate GetEnumerableParseStringSpanFn() => DeserializeListWithElements<TSerializer>.ParseStringList;

        public static ParseStringDelegate GetGenericEnumerableParseFn() => v => GetGenericEnumerableParseStringSpanFn()(v.AsSpan());

        public static ParseStringSpanDelegate GetGenericEnumerableParseStringSpanFn()
        {
            var enumerableInterface = typeof(T).GetTypeWithGenericInterfaceOf(typeof(IEnumerable<>));
            if (enumerableInterface == null) return null;
            var elementType = enumerableInterface.GetGenericArguments()[0];
            var genericType = typeof(SpecializedEnumerableElements<,>).MakeGenericType(typeof(T), elementType);
            var fi = genericType.GetPublicStaticField("ConvertFn");

            var convertFn = fi.GetValue(null) as ConvertObjectDelegate;
            if (convertFn == null) return null;

            var parseFn = DeserializeEnumerable<T, TSerializer>.GetParseStringSpanFn();

            return x => convertFn(parseFn(x));
        }
    }

    internal class SpecializedQueueElements<T>
    {
        public static Queue<T> ConvertToQueue(object enumerable)
        {
            if (enumerable == null) return null;
            return new Queue<T>((IEnumerable<T>)enumerable);
        }

        public static Stack<T> ConvertToStack(object enumerable)
        {
            if (enumerable == null) return null;
            return new Stack<T>((IEnumerable<T>)enumerable);
        }
    }

    internal class SpecializedEnumerableElements<TCollection, T>
    {
        public static ConvertObjectDelegate ConvertFn;

        static SpecializedEnumerableElements()
        {
            foreach (var ctorInfo in typeof(TCollection).GetConstructors())
            {
                var ctorParams = ctorInfo.GetParameters();
                if (ctorParams.Length != 1) continue;
                var ctorParam = ctorParams[0];

                if (typeof(IEnumerable).IsAssignableFrom(ctorParam.ParameterType)
                    || ctorParam.ParameterType.IsOrHasGenericInterfaceTypeOf(typeof(IEnumerable<>)))
                {
                    ConvertFn = fromObject =>
                    {
                        var to = Activator.CreateInstance(typeof(TCollection), fromObject);
                        return to;
                    };
                    return;
                }
            }

            if (typeof(TCollection).IsOrHasGenericInterfaceTypeOf(typeof(ICollection<>)))
            {
                ConvertFn = ConvertFromCollection;
            }
        }

        public static object Convert(object enumerable)
        {
            return ConvertFn(enumerable);
        }

        public static object ConvertFromCollection(object enumerable)
        {
            var to = (ICollection<T>)typeof(TCollection).CreateInstance();
            var from = (IEnumerable<T>)enumerable;
            foreach (var item in from)
            {
                to.Add(item);
            }
            return to;
        }
    }
}
