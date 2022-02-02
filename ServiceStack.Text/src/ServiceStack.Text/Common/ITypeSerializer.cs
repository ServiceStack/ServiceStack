using System;
using System.IO;
using ServiceStack.Text.Json;

namespace ServiceStack.Text.Common
{
    public delegate object ObjectDeserializerDelegate(ReadOnlySpan<char> value);
    
    public interface ITypeSerializer
    {
        ObjectDeserializerDelegate ObjectDeserializer { get; set; }

        bool IncludeNullValues { get; }
        bool IncludeNullValuesInDictionaries { get; }
        string TypeAttrInObject { get; }

        WriteObjectDelegate GetWriteFn<T>();
        WriteObjectDelegate GetWriteFn(Type type);
        TypeInfo GetTypeInfo(Type type);

        void WriteRawString(TextWriter writer, string value);
        void WritePropertyName(TextWriter writer, string value);

        void WriteBuiltIn(TextWriter writer, object value);
        void WriteObjectString(TextWriter writer, object value);
        void WriteException(TextWriter writer, object value);
        void WriteString(TextWriter writer, string value);
        void WriteFormattableObjectString(TextWriter writer, object value);
        void WriteDateTime(TextWriter writer, object oDateTime);
        void WriteNullableDateTime(TextWriter writer, object dateTime);
        void WriteDateTimeOffset(TextWriter writer, object oDateTimeOffset);
        void WriteNullableDateTimeOffset(TextWriter writer, object dateTimeOffset);
        void WriteTimeSpan(TextWriter writer, object timeSpan);
        void WriteNullableTimeSpan(TextWriter writer, object timeSpan);
        void WriteGuid(TextWriter writer, object oValue);
        void WriteNullableGuid(TextWriter writer, object oValue);
        void WriteBytes(TextWriter writer, object oByteValue);
        void WriteChar(TextWriter writer, object charValue);
        void WriteByte(TextWriter writer, object byteValue);
        void WriteSByte(TextWriter writer, object sbyteValue);
        void WriteInt16(TextWriter writer, object intValue);
        void WriteUInt16(TextWriter writer, object intValue);
        void WriteInt32(TextWriter writer, object intValue);
        void WriteUInt32(TextWriter writer, object uintValue);
        void WriteInt64(TextWriter writer, object longValue);
        void WriteUInt64(TextWriter writer, object ulongValue);
        void WriteBool(TextWriter writer, object boolValue);
        void WriteFloat(TextWriter writer, object floatValue);
        void WriteDouble(TextWriter writer, object doubleValue);
        void WriteDecimal(TextWriter writer, object decimalValue);
        void WriteEnum(TextWriter writer, object enumValue);

#if NET6_0_OR_GREATER
        void WriteDateOnly(TextWriter writer, object oDateOnly);
        void WriteNullableDateOnly(TextWriter writer, object oDateOnly);
        void WriteTimeOnly(TextWriter writer, object oTimeOnly);
        void WriteNullableTimeOnly(TextWriter writer, object oTimeOnly);
#endif

        ParseStringDelegate GetParseFn<T>();
        ParseStringSpanDelegate GetParseStringSpanFn<T>();
        ParseStringDelegate GetParseFn(Type type);
        ParseStringSpanDelegate GetParseStringSpanFn(Type type);

        string ParseRawString(string value);
        string ParseString(string value);
        string ParseString(ReadOnlySpan<char> value);
        string UnescapeString(string value);
        ReadOnlySpan<char> UnescapeString(ReadOnlySpan<char> value);
        object UnescapeStringAsObject(ReadOnlySpan<char> value);
        string UnescapeSafeString(string value);
        ReadOnlySpan<char> UnescapeSafeString(ReadOnlySpan<char> value);
        string EatTypeValue(string value, ref int i);
        ReadOnlySpan<char> EatTypeValue(ReadOnlySpan<char> value, ref int i);
        bool EatMapStartChar(string value, ref int i);
        bool EatMapStartChar(ReadOnlySpan<char> value, ref int i);
        string EatMapKey(string value, ref int i);
        ReadOnlySpan<char> EatMapKey(ReadOnlySpan<char> value, ref int i);
        bool EatMapKeySeperator(string value, ref int i);
        bool EatMapKeySeperator(ReadOnlySpan<char> value, ref int i);
        void EatWhitespace(string value, ref int i);
        void EatWhitespace(ReadOnlySpan<char> value, ref int i);
        string EatValue(string value, ref int i);
        ReadOnlySpan<char> EatValue(ReadOnlySpan<char> value, ref int i);
        bool EatItemSeperatorOrMapEndChar(string value, ref int i);
        bool EatItemSeperatorOrMapEndChar(ReadOnlySpan<char> value, ref int i);
    }
}