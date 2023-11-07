using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using ServiceStack.Text.Json;
using ServiceStack.Text.Jsv;

namespace ServiceStack.Text.Common;

public static class JsWriter
{
    public const string TypeAttr = "__type";

    public const char MapStartChar = '{';
    public const char MapKeySeperator = ':';
    public const char ItemSeperator = ',';
    public const char MapEndChar = '}';
    public const string MapNullValue = "\"\"";
    public const string EmptyMap = "{}";

    public const char ListStartChar = '[';
    public const char ListEndChar = ']';
    public const char ReturnChar = '\r';
    public const char LineFeedChar = '\n';

    public const char QuoteChar = '"';
    public const string QuoteString = "\"";
    public const string EscapedQuoteString = "\\\"";
    public const string ItemSeperatorString = ",";
    public const string MapKeySeperatorString = ":";

    public static readonly char[] CsvChars = { ItemSeperator, QuoteChar };
    public static readonly char[] EscapeChars = { QuoteChar, MapKeySeperator, ItemSeperator, MapStartChar, MapEndChar, ListStartChar, ListEndChar, ReturnChar, LineFeedChar };

    private const int LengthFromLargestChar = '}' + 1;
    private static readonly bool[] EscapeCharFlags = new bool[LengthFromLargestChar];

    static JsWriter()
    {
        foreach (var escapeChar in EscapeChars)
        {
            EscapeCharFlags[escapeChar] = true;
        }
        var loadConfig = JsConfig.TextCase; //force load
    }

    public static void WriteDynamic(Action callback)
    {
        var prevState = JsState.IsWritingDynamic;
        JsState.IsWritingDynamic = true;
        try
        {
            callback();
        }
        finally
        {
            JsState.IsWritingDynamic = prevState;
        }
    }

    /// <summary>
    /// micro optimizations: using flags instead of value.IndexOfAny(EscapeChars)
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static bool HasAnyEscapeChars(string value)
    {
        var len = value.Length;
        for (var i = 0; i < len; i++)
        {
            var c = value[i];
            if (c >= LengthFromLargestChar || !EscapeCharFlags[c]) continue;
            return true;
        }
        return false;
    }

    internal static void WriteItemSeperatorIfRanOnce(TextWriter writer, ref bool ranOnce)
    {
        if (ranOnce)
            writer.Write(ItemSeperator);
        else
            ranOnce = true;
    }

    internal static bool ShouldUseDefaultToStringMethod(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        switch (underlyingType.GetTypeCode())
        {
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
            case TypeCode.DateTime:
                return true;
        }

        return underlyingType == typeof(Guid);
    }

    public static ITypeSerializer GetTypeSerializer<TSerializer>()
    {
        if (typeof(TSerializer) == typeof(JsvTypeSerializer))
            return JsvTypeSerializer.Instance;

        if (typeof(TSerializer) == typeof(JsonTypeSerializer))
            return JsonTypeSerializer.Instance;

        throw new NotSupportedException(typeof(TSerializer).Name);
    }

    public static void WriteEnumFlags(TextWriter writer, object enumFlagValue)
    {
        if (enumFlagValue == null) return;

        var typeCode = Enum.GetUnderlyingType(enumFlagValue.GetType()).GetTypeCode();
        switch (typeCode)
        {
            case TypeCode.SByte:
                writer.Write((sbyte)enumFlagValue);
                break;
            case TypeCode.Byte:
                writer.Write((byte)enumFlagValue);
                break;
            case TypeCode.Int16:
                writer.Write((short)enumFlagValue);
                break;
            case TypeCode.UInt16:
                writer.Write((ushort)enumFlagValue);
                break;
            case TypeCode.Int32:
                writer.Write((int)enumFlagValue);
                break;
            case TypeCode.UInt32:
                writer.Write((uint)enumFlagValue);
                break;
            case TypeCode.Int64:
                writer.Write((long)enumFlagValue);
                break;
            case TypeCode.UInt64:
                writer.Write((ulong)enumFlagValue);
                break;
            default:
                writer.Write((int)enumFlagValue);
                break;
        }
    }

    public static bool ShouldAllowRuntimeType(Type type)
    {
        if (type.IsInterface && JsConfig.AllowRuntimeInterfaces)
            return true;
            
        if (JsConfig.AllowRuntimeType?.Invoke(type) == true)
            return true;

        var allowAttributesNamed = JsConfig.AllowRuntimeTypeWithAttributesNamed;
        if (allowAttributesNamed?.Count > 0)
        {
            var oAttrs = type.AllAttributes();
            foreach (var oAttr in oAttrs)
            {
                if (oAttr is not Attribute attr) continue;
                if (allowAttributesNamed.Contains(attr.GetType().Name))
                    return true;
            }
        }

        var allowInterfacesNamed = JsConfig.AllowRuntimeTypeWithInterfacesNamed;
        if (allowInterfacesNamed?.Count > 0)
        {
            var interfaces = type.GetInterfaces();
            foreach (var interfaceType in interfaces)
            {
                if (allowInterfacesNamed.Contains(interfaceType.Name))
                    return true;
            }
        }

        var allowTypesInNamespaces = JsConfig.AllowRuntimeTypeInTypesWithNamespaces;
        if (allowTypesInNamespaces?.Count > 0)
        {
            foreach (var ns in allowTypesInNamespaces)
            {
                if (type.Namespace == ns)
                    return true;
            }
        }

        var allowRuntimeTypeInTypes = JsConfig.AllowRuntimeTypeInTypes;
        var declaringTypeName = JsState.DeclaringType?.FullName;
        if (allowRuntimeTypeInTypes?.Count > 0 && declaringTypeName != null)
        {
            foreach (var allowInType in allowRuntimeTypeInTypes)
            {
                if (declaringTypeName == allowInType)
                    return true;
            }
        }
            
        return false;
    }

    public static void AssertAllowedRuntimeType(Type type)
    {
        if (!ShouldAllowRuntimeType(type))
            throw new NotSupportedException($"{type.Name} is not an allowed Runtime Type. Whitelist Type with [Serializable], [RuntimeSerializable], [DataContract] or IRuntimeSerializable, see: https://docs.servicestack.net/json-format#runtime-type-whitelist");
    }
}

public class JsWriter<TSerializer>
    where TSerializer : ITypeSerializer
{
    private static readonly ITypeSerializer Serializer = JsWriter.GetTypeSerializer<TSerializer>();

    public JsWriter()
    {
        this.SpecialTypes = new Dictionary<Type, WriteObjectDelegate>
        {
            { typeof(Uri), Serializer.WriteObjectString },
            { typeof(Type), WriteType },
            { typeof(Exception), Serializer.WriteException },
        };
    }

    public WriteObjectDelegate GetValueTypeToStringMethod(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type);
        var isNullable = underlyingType != null;
        underlyingType ??= type;

        if (!underlyingType.IsEnum)
        {
            var typeCode = underlyingType.GetTypeCode();

            if (typeCode == TypeCode.Char)
                return Serializer.WriteChar;
            if (typeCode == TypeCode.Int32)
                return Serializer.WriteInt32;
            if (typeCode == TypeCode.Int64)
                return Serializer.WriteInt64;
            if (typeCode == TypeCode.UInt64)
                return Serializer.WriteUInt64;
            if (typeCode == TypeCode.UInt32)
                return Serializer.WriteUInt32;

            if (typeCode == TypeCode.Byte)
                return Serializer.WriteByte;
            if (typeCode == TypeCode.SByte)
                return Serializer.WriteSByte;

            if (typeCode == TypeCode.Int16)
                return Serializer.WriteInt16;
            if (typeCode == TypeCode.UInt16)
                return Serializer.WriteUInt16;

            if (typeCode == TypeCode.Boolean)
                return Serializer.WriteBool;

            if (typeCode == TypeCode.Single)
                return Serializer.WriteFloat;

            if (typeCode == TypeCode.Double)
                return Serializer.WriteDouble;

            if (typeCode == TypeCode.Decimal)
                return Serializer.WriteDecimal;

            if (typeCode == TypeCode.DateTime)
                if (isNullable)
                    return Serializer.WriteNullableDateTime;
                else
                    return Serializer.WriteDateTime;

            if (type == typeof(DateTimeOffset))
                return Serializer.WriteDateTimeOffset;

            if (type == typeof(DateTimeOffset?))
                return Serializer.WriteNullableDateTimeOffset;

            if (type == typeof(TimeSpan))
                return Serializer.WriteTimeSpan;

            if (type == typeof(TimeSpan?))
                return Serializer.WriteNullableTimeSpan;

            if (type == typeof(Guid))
                return Serializer.WriteGuid;

            if (type == typeof(Guid?))
                return Serializer.WriteNullableGuid;

#if NET6_0_OR_GREATER
                if (type == typeof(DateOnly))
                    if (isNullable)
                        return Serializer.WriteNullableDateOnly;
                    else
                        return Serializer.WriteDateOnly;
                if (type == typeof(DateOnly?))
                    return Serializer.WriteDateOnly;
                
                if (type == typeof(TimeOnly))
                    if (isNullable)
                        return Serializer.WriteNullableTimeOnly;
                    else
                        return Serializer.WriteTimeOnly;
                if (type == typeof(TimeOnly?))
                    return Serializer.WriteTimeOnly;
#endif
        }
        else
        {
            if (underlyingType.IsEnum)
            {
                return Serializer.WriteEnum;
            }
        }

        if (type.HasInterface(typeof(IFormattable)))
            return Serializer.WriteFormattableObjectString;

        if (type.HasInterface(typeof(IValueWriter)))
            return WriteValue;

        return Serializer.WriteObjectString;
    }

    public WriteObjectDelegate GetWriteFn<T>()
    {
        if (typeof(T) == typeof(string))
        {
            return Serializer.WriteObjectString;
        }

        WriteObjectDelegate ret = null;

        var onSerializingFn = JsConfig<T>.OnSerializingFn;
        if (onSerializingFn != null)
        {
            var writeFn = GetCoreWriteFn<T>();
            ret = (w, x) => writeFn(w, onSerializingFn((T)x));
        }

        if (JsConfig<T>.HasSerializeFn)
        {
            ret = JsConfig<T>.WriteFn<TSerializer>;
        }

        if (ret == null)
        {
            ret = GetCoreWriteFn<T>();
        }

        var onSerializedFn = JsConfig<T>.OnSerializedFn;
        if (onSerializedFn != null)
        {
            var writerFunc = ret;
            ret = (w, x) =>
            {
                writerFunc(w, x);
                onSerializedFn((T)x);
            };
        }

        return ret;
    }

    public void WriteValue(TextWriter writer, object value)
    {
        var valueWriter = (IValueWriter)value;
        valueWriter.WriteTo(Serializer, writer);
    }
        
    void ThrowTaskNotSupported(TextWriter writer, object value) =>
        throw new NotSupportedException("Serializing Task's is not supported. Did you forget to await it?");

    private WriteObjectDelegate GetCoreWriteFn<T>()
    {
        if (typeof(T).IsInstanceOf(typeof(System.Threading.Tasks.Task)))
            return ThrowTaskNotSupported;
            
        if (typeof(T).IsValueType && !JsConfig.TreatAsRefType(typeof(T)) || JsConfig<T>.HasSerializeFn)
        {
            return JsConfig<T>.HasSerializeFn
                ? JsConfig<T>.WriteFn<TSerializer>
                : GetValueTypeToStringMethod(typeof(T));
        }

        var specialWriteFn = GetSpecialWriteFn(typeof(T));
        if (specialWriteFn != null)
        {
            return specialWriteFn;
        }

        if (typeof(T).IsArray)
        {
            if (typeof(T) == typeof(byte[]))
                return (w, x) => WriteLists.WriteBytes(Serializer, w, x);

            if (typeof(T) == typeof(string[]))
                return (w, x) => WriteLists.WriteStringArray(Serializer, w, x);

            if (typeof(T) == typeof(int[]))
                return WriteListsOfElements<int, TSerializer>.WriteGenericArrayValueType;
            if (typeof(T) == typeof(long[]))
                return WriteListsOfElements<long, TSerializer>.WriteGenericArrayValueType;

            var elementType = typeof(T).GetElementType();
            var writeFn = WriteListsOfElements<TSerializer>.GetGenericWriteArray(elementType);
            return writeFn;
        }

        if (typeof(T).HasGenericType() ||
            typeof(T).HasInterface(typeof(IDictionary<string, object>))) // is ExpandoObject?
        {
            if (typeof(T).IsOrHasGenericInterfaceTypeOf(typeof(IList<>)))
                return WriteLists<T, TSerializer>.Write;

            var mapInterface = typeof(T).GetTypeWithGenericTypeDefinitionOf(typeof(IDictionary<,>));
            if (mapInterface != null)
            {
                var mapTypeArgs = mapInterface.GetGenericArguments();
                var writeFn = WriteDictionary<TSerializer>.GetWriteGenericDictionary(
                    mapTypeArgs[0], mapTypeArgs[1]);

                var keyWriteFn = Serializer.GetWriteFn(mapTypeArgs[0]);
                var valueWriteFn = typeof(JsonObject).IsAssignableFrom(typeof(T))
                    ? JsonObject.WriteValue
                    : Serializer.GetWriteFn(mapTypeArgs[1]);

                return (w, x) => writeFn(w, x, keyWriteFn, valueWriteFn);
            }
        }

        var enumerableInterface = typeof(T).GetTypeWithGenericTypeDefinitionOf(typeof(IEnumerable<>));
        if (enumerableInterface != null)
        {
            var elementType = enumerableInterface.GetGenericArguments()[0];
            var writeFn = WriteListsOfElements<TSerializer>.GetGenericWriteEnumerable(elementType);
            return writeFn;
        }

        var isDictionary = typeof(T) != typeof(IEnumerable) && typeof(T) != typeof(ICollection)
                                                            && (typeof(T).IsAssignableFrom(typeof(IDictionary)) || typeof(T).HasInterface(typeof(IDictionary)));
        if (isDictionary)
        {
            return WriteDictionary<TSerializer>.WriteIDictionary;
        }

        var isEnumerable = typeof(T).IsAssignableFrom(typeof(IEnumerable))
                           || typeof(T).HasInterface(typeof(IEnumerable));
        if (isEnumerable)
        {
            return WriteListsOfElements<TSerializer>.WriteIEnumerable;
        }

        if (typeof(T).HasInterface(typeof(IValueWriter)))
            return WriteValue;

        if (typeof(T).IsClass || typeof(T).IsInterface || JsConfig.TreatAsRefType(typeof(T)))
        {
            var typeToStringMethod = WriteType<T, TSerializer>.Write;
            if (typeToStringMethod != null)
            {
                return typeToStringMethod;
            }
        }

        return Serializer.WriteBuiltIn;
    }

    public readonly Dictionary<Type, WriteObjectDelegate> SpecialTypes;

    public WriteObjectDelegate GetSpecialWriteFn(Type type)
    {
        if (SpecialTypes.TryGetValue(type, out var writeFn))
            return writeFn;

        if (type.IsInstanceOfType(typeof(Type)))
            return WriteType;

        if (type.IsInstanceOf(typeof(Exception)))
            return Serializer.WriteException;

        return null;
    }

    public void WriteType(TextWriter writer, object value)
    {
        Serializer.WriteRawString(writer, JsConfig.TypeWriter((Type)value));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    public static void InitAot<T>()
    {
        WriteListsOfElements<T, TSerializer>.WriteList(null, null);
        WriteListsOfElements<T, TSerializer>.WriteIList(null, null);
        WriteListsOfElements<T, TSerializer>.WriteEnumerable(null, null);
        WriteListsOfElements<T, TSerializer>.WriteListValueType(null, null);
        WriteListsOfElements<T, TSerializer>.WriteIListValueType(null, null);
        WriteListsOfElements<T, TSerializer>.WriteGenericArrayValueType(null, null);
        WriteListsOfElements<T, TSerializer>.WriteArray(null, null);

        TranslateListWithElements<T>.LateBoundTranslateToGenericICollection(null, null);
        TranslateListWithConvertibleElements<T, T>.LateBoundTranslateToGenericICollection(null, null);

        QueryStringWriter<T>.WriteObject(null, null);
    }
}