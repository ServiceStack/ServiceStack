using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ServiceStack.Text.Common
{
    public class JsReader<TSerializer>
        where TSerializer : ITypeSerializer
    {
        private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

        public ParseStringDelegate GetParseFn<T>()
        {
            var onDeserializedFn = JsConfig<T>.OnDeserializedFn;
            if (onDeserializedFn != null)
            {
                var parseFn = GetCoreParseFn<T>();
                return value => onDeserializedFn((T)parseFn(value));
            }

            return GetCoreParseFn<T>();
        }

        public ParseStringSpanDelegate GetParseStringSpanFn<T>()
        {
            var onDeserializedFn = JsConfig<T>.OnDeserializedFn;
            if (onDeserializedFn != null)
            {
                var parseFn = GetCoreParseStringSpanFn<T>();
                return value => onDeserializedFn((T)parseFn(value));
            }

            return GetCoreParseStringSpanFn<T>();
        }

        private ParseStringDelegate GetCoreParseFn<T>()
        {
            return v => GetCoreParseStringSpanFn<T>()(v.AsSpan());
        }

        private ParseStringSpanDelegate GetCoreParseStringSpanFn<T>()
        {
            var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            if (JsConfig<T>.HasDeserializeFn)
                return value => JsConfig<T>.ParseFn(Serializer, value.Value());

            if (type.IsEnum)
                return x => ParseUtils.TryParseEnum(type, Serializer.UnescapeSafeString(x).Value());

            if (type == typeof(string))
                return Serializer.UnescapeStringAsObject;

            if (type == typeof(object))
                return DeserializeType<TSerializer>.ObjectStringToType;

            var specialParseFn = ParseUtils.GetSpecialParseMethod(type);
            if (specialParseFn != null)
                return v => specialParseFn(v.Value());

            if (type.IsArray)
            {
                return DeserializeArray<T, TSerializer>.ParseStringSpan;
            }

            var builtInMethod = DeserializeBuiltin<T>.ParseStringSpan;
            if (builtInMethod != null)
                return value => builtInMethod(Serializer.UnescapeSafeString(value));

            if (type.HasGenericType())
            {
                if (type.IsOrHasGenericInterfaceTypeOf(typeof(IList<>)))
                    return DeserializeList<T, TSerializer>.ParseStringSpan;

                if (type.IsOrHasGenericInterfaceTypeOf(typeof(IDictionary<,>)))
                    return DeserializeDictionary<TSerializer>.GetParseStringSpanMethod(type);

                if (type.IsOrHasGenericInterfaceTypeOf(typeof(ICollection<>)))
                    return DeserializeCollection<TSerializer>.GetParseStringSpanMethod(type);

                if (type.HasAnyTypeDefinitionsOf(typeof(Queue<>))
                    || type.HasAnyTypeDefinitionsOf(typeof(Stack<>)))
                    return DeserializeSpecializedCollections<T, TSerializer>.ParseStringSpan;

                if (type.IsOrHasGenericInterfaceTypeOf(typeof(KeyValuePair<,>)))
                    return DeserializeKeyValuePair<TSerializer>.GetParseStringSpanMethod(type);

                if (type.IsOrHasGenericInterfaceTypeOf(typeof(IEnumerable<>)))
                    return DeserializeEnumerable<T, TSerializer>.ParseStringSpan;

                var customFn = DeserializeCustomGenericType<TSerializer>.GetParseStringSpanMethod(type);
                if (customFn != null)
                    return customFn;
            }

            var pclParseFn = PclExport.Instance.GetJsReaderParseStringSpanMethod<TSerializer>(typeof(T));
            if (pclParseFn != null)
                return pclParseFn;

            var isDictionary = typeof(T) != typeof(IEnumerable) && typeof(T) != typeof(ICollection)
                && (typeof(T).IsAssignableFrom(typeof(IDictionary)) || typeof(T).HasInterface(typeof(IDictionary)));
            if (isDictionary)
            {
                return DeserializeDictionary<TSerializer>.GetParseStringSpanMethod(type);
            }

            var isEnumerable = typeof(T).IsAssignableFrom(typeof(IEnumerable))
                || typeof(T).HasInterface(typeof(IEnumerable));
            if (isEnumerable)
            {
                var parseFn = DeserializeSpecializedCollections<T, TSerializer>.ParseStringSpan;
                if (parseFn != null) 
                    return parseFn;
            }

            if (type.IsValueType)
            {
                //at first try to find more faster `ParseStringSpan` method
                var staticParseStringSpanMethod = StaticParseMethod<T>.ParseStringSpan;
                if (staticParseStringSpanMethod != null)
                    return value => staticParseStringSpanMethod(Serializer.UnescapeSafeString(value));
                
                //then try to find `Parse` method
                var staticParseMethod = StaticParseMethod<T>.Parse;
                if (staticParseMethod != null)
                    return value => staticParseMethod(Serializer.UnescapeSafeString(value).ToString());
            }
            else
            {
                var staticParseStringSpanMethod = StaticParseRefTypeMethod<TSerializer, T>.ParseStringSpan;
                if (staticParseStringSpanMethod != null)
                    return value => staticParseStringSpanMethod(Serializer.UnescapeSafeString(value));

                var staticParseMethod = StaticParseRefTypeMethod<TSerializer, T>.Parse;
                if (staticParseMethod != null)
                    return value => staticParseMethod(Serializer.UnescapeSafeString(value).ToString());
            }

            var typeConstructor = DeserializeType<TSerializer>.GetParseStringSpanMethod(TypeConfig<T>.GetState());
            if (typeConstructor != null)
                return typeConstructor;

            var stringConstructor = DeserializeTypeUtils.GetParseStringSpanMethod(type);
            if (stringConstructor != null) 
                return stringConstructor;

            return DeserializeType<TSerializer>.ParseAbstractType<T>;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void InitAot<T>()
        {
            var hold = DeserializeBuiltin<T>.Parse;
            hold = DeserializeArray<T[], TSerializer>.Parse;
            DeserializeType<TSerializer>.ExtractType(default(ReadOnlySpan<char>));
            DeserializeArrayWithElements<T, TSerializer>.ParseGenericArray(default(ReadOnlySpan<char>), null);
            DeserializeCollection<TSerializer>.ParseCollection<T>(default(ReadOnlySpan<char>), null, null);
            DeserializeListWithElements<T, TSerializer>.ParseGenericList(default(ReadOnlySpan<char>), null, null);
        }
    }
}
