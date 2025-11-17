//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using ServiceStack.Text.Common;
using ServiceStack.Text.Json;

namespace ServiceStack.Text.Jsv;

public struct JsvTypeSerializer
    : ITypeSerializer
{
    public static ITypeSerializer Instance = new JsvTypeSerializer();

    public ObjectDeserializerDelegate ObjectDeserializer { get; set; }

    public bool IncludeNullValues => false;

    public bool IncludeNullValuesInDictionaries => false;

    public string TypeAttrInObject => JsConfig.JsvTypeAttrInObject;

    internal static string GetTypeAttrInObject(string typeAttr) => $"{{{typeAttr}:";

    public WriteObjectDelegate GetWriteFn<T>() => JsvWriter<T>.WriteFn();

    public WriteObjectDelegate GetWriteFn(Type type) => JsvWriter.GetWriteFn(type);

    static readonly TypeInfo DefaultTypeInfo = new TypeInfo { EncodeMapKey = false };

    public TypeInfo GetTypeInfo(Type type) => DefaultTypeInfo;

    public void WriteRawString(TextWriter writer, string value)
    {
        writer.Write(value.EncodeJsv());
    }

    public void WritePropertyName(TextWriter writer, string value)
    {
        writer.Write(value);
    }

    public void WriteBuiltIn(TextWriter writer, object value)
    {
        writer.Write(value);
    }

    public void WriteObjectString(TextWriter writer, object value)
    {
        if (value != null)
        {
            if (value is string strValue)
            {
                WriteString(writer, strValue);
            }
            else
            {
                writer.Write(value.ToString().EncodeJsv());
            }
        }
    }

    public void WriteException(TextWriter writer, object value)
    {
        writer.Write(((Exception)value).Message.EncodeJsv());
    }

    public void WriteString(TextWriter writer, string value)
    {
        if (JsState.QueryStringMode && !string.IsNullOrEmpty(value) && value.StartsWith(JsWriter.QuoteString) && value.EndsWith(JsWriter.QuoteString))
            value = String.Concat(JsWriter.QuoteChar, value, JsWriter.QuoteChar);
        else if (JsState.QueryStringMode && !string.IsNullOrEmpty(value) && value.Contains(JsWriter.ItemSeperatorString))
            value = String.Concat(JsWriter.QuoteChar, value, JsWriter.QuoteChar);

        writer.Write(value == "" ? "\"\"" : value.EncodeJsv());
    }

    public void WriteFormattableObjectString(TextWriter writer, object value)
    {
        var f = (IFormattable)value;
        writer.Write(f.ToString(null, CultureInfo.InvariantCulture).EncodeJsv());
    }

    public void WriteDateTime(TextWriter writer, object oDateTime)
    {
        var dateTime = (DateTime)oDateTime;
        switch (JsConfig.DateHandler)
        {
            case DateHandler.UnixTime:
                writer.Write(dateTime.ToUnixTime());
                return;
            case DateHandler.UnixTimeMs:
                writer.Write(dateTime.ToUnixTimeMs());
                return;
        }

        writer.Write(DateTimeSerializer.ToShortestXsdDateTimeString((DateTime)oDateTime));
    }

    public void WriteNullableDateTime(TextWriter writer, object dateTime)
    {
        if (dateTime == null) return;
        WriteDateTime(writer, dateTime);
    }

    public void WriteDateTimeOffset(TextWriter writer, object oDateTimeOffset)
    {
        writer.Write(((DateTimeOffset)oDateTimeOffset).ToString("o"));
    }

    public void WriteNullableDateTimeOffset(TextWriter writer, object dateTimeOffset)
    {
        if (dateTimeOffset == null) return;
        this.WriteDateTimeOffset(writer, dateTimeOffset);
    }

    public void WriteTimeSpan(TextWriter writer, object oTimeSpan)
    {
        writer.Write(DateTimeSerializer.ToXsdTimeSpanString((TimeSpan)oTimeSpan));
    }

    public void WriteNullableTimeSpan(TextWriter writer, object oTimeSpan)
    {
        if (oTimeSpan == null) return;
        writer.Write(DateTimeSerializer.ToXsdTimeSpanString((TimeSpan?)oTimeSpan));
    }

    public void WriteGuid(TextWriter writer, object oValue)
    {
        var formatted = JsConfig.SystemJsonCompatible
            ? ((Guid)oValue).ToString("D")
            : ((Guid)oValue).ToString("N");
        writer.Write(formatted);
    }

    public void WriteNullableGuid(TextWriter writer, object oValue)
    {
        if (oValue == null) return;
        WriteGuid(writer, oValue);
    }

    public void WriteBytes(TextWriter writer, object oByteValue)
    {
        if (oByteValue == null) return;
        writer.Write(Convert.ToBase64String((byte[])oByteValue));
    }

    public void WriteChar(TextWriter writer, object charValue)
    {
        if (charValue == null) return;
        writer.Write((char)charValue);
    }

    public void WriteByte(TextWriter writer, object byteValue)
    {
        if (byteValue == null) return;
        writer.Write((byte)byteValue);
    }

    public void WriteSByte(TextWriter writer, object sbyteValue)
    {
        if (sbyteValue == null) return;
        writer.Write((sbyte)sbyteValue);
    }

    public void WriteInt16(TextWriter writer, object intValue)
    {
        if (intValue == null) return;
        writer.Write((short)intValue);
    }

    public void WriteUInt16(TextWriter writer, object intValue)
    {
        if (intValue == null) return;
        writer.Write((ushort)intValue);
    }

    public void WriteInt32(TextWriter writer, object intValue)
    {
        if (intValue == null) return;
        writer.Write((int)intValue);
    }

    public void WriteUInt32(TextWriter writer, object uintValue)
    {
        if (uintValue == null) return;
        writer.Write((uint)uintValue);
    }

    public void WriteUInt64(TextWriter writer, object ulongValue)
    {
        if (ulongValue == null) return;
        writer.Write((ulong)ulongValue);
    }

    public void WriteInt64(TextWriter writer, object longValue)
    {
        if (longValue == null) return;
        writer.Write((long)longValue);
    }

    public void WriteBool(TextWriter writer, object boolValue)
    {
        if (boolValue == null) return;
        writer.Write((bool)boolValue);
    }

    public void WriteFloat(TextWriter writer, object floatValue)
    {
        if (floatValue == null) return;
        var floatVal = (float)floatValue;
        var cultureInfo = JsState.IsCsv ? CsvConfig.RealNumberCultureInfo : null;

        if (Equals(floatVal, float.MaxValue) || Equals(floatVal, float.MinValue))
            writer.Write(floatVal.ToString("r", cultureInfo ?? CultureInfo.InvariantCulture));
        else
            writer.Write(floatVal.ToString(cultureInfo ?? CultureInfo.InvariantCulture));
    }

    public void WriteDouble(TextWriter writer, object doubleValue)
    {
        if (doubleValue == null) return;
        var doubleVal = (double)doubleValue;
        var cultureInfo = JsState.IsCsv ? CsvConfig.RealNumberCultureInfo : null;

        if (Equals(doubleVal, double.MaxValue) || Equals(doubleVal, double.MinValue))
            writer.Write(doubleVal.ToString("r", cultureInfo ?? CultureInfo.InvariantCulture));
        else
            writer.Write(doubleVal.ToString(cultureInfo ?? CultureInfo.InvariantCulture));
    }

    public void WriteDecimal(TextWriter writer, object decimalValue)
    {
        if (decimalValue == null) return;
        var cultureInfo = JsState.IsCsv ? CsvConfig.RealNumberCultureInfo : null;

        writer.Write(((decimal)decimalValue).ToString(cultureInfo ?? CultureInfo.InvariantCulture));
    }

    public void WriteEnum(TextWriter writer, object enumValue)
    {
        if (enumValue == null) 
            return;
        var serializedValue = CachedTypeInfo.Get(enumValue.GetType()).EnumInfo.GetSerializedValue(enumValue);
        if (serializedValue is string strEnum)
            writer.Write(strEnum);
        else
            JsWriter.WriteEnumFlags(writer, enumValue);
    }

#if NET6_0_OR_GREATER
        public void WriteDateOnly(TextWriter writer, object oDateOnly)
        {
            var dateOnly = (DateOnly)oDateOnly;
            switch (JsConfig.DateHandler)
            {
                case DateHandler.UnixTime:
                    writer.Write(dateOnly.ToUnixTime());
                    break;
                case DateHandler.UnixTimeMs:
                    writer.Write(dateOnly.ToUnixTimeMs());
                    break;
                default:
                    writer.Write(dateOnly.ToString("O"));
                    break;
            }
        }

        public void WriteNullableDateOnly(TextWriter writer, object oDateOnly)
        {
            if (oDateOnly == null) return;
            WriteDateOnly(writer, oDateOnly);
        }

        public void WriteTimeOnly(TextWriter writer, object oTimeOnly)
        {
            var stringValue = JsConfig.TimeSpanHandler == TimeSpanHandler.StandardFormat
                ? oTimeOnly.ToString()
                : DateTimeSerializer.ToXsdTimeSpanString(((TimeOnly)oTimeOnly).ToTimeSpan());
            WriteRawString(writer, stringValue);
        }

        public void WriteNullableTimeOnly(TextWriter writer, object oTimeOnly)
        {
            if (oTimeOnly == null) return;
            WriteTimeSpan(writer, ((TimeOnly?)oTimeOnly).Value.ToTimeSpan());
        }
#endif        

    public ParseStringDelegate GetParseFn<T>() => JsvReader.Instance.GetParseFn<T>();

    public ParseStringDelegate GetParseFn(Type type) => JsvReader.GetParseFn(type);

    public ParseStringSpanDelegate GetParseStringSpanFn<T>() => JsvReader.Instance.GetParseStringSpanFn<T>();

    public ParseStringSpanDelegate GetParseStringSpanFn(Type type) => JsvReader.GetParseStringSpanFn(type);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public object UnescapeStringAsObject(ReadOnlySpan<char> value)
    {
        return UnescapeSafeString(value).Value();
    }

    public string UnescapeSafeString(string value) => JsState.IsCsv
        ? value
        : value.FromCsvField().ToString();

    public ReadOnlySpan<char> UnescapeSafeString(ReadOnlySpan<char> value) => JsState.IsCsv
        ? value // already unescaped in CsvReader.ParseFields()
        : value.FromCsvField();

    public string ParseRawString(string value) => value;

    public string ParseString(string value) => value.FromCsvField().ToString();

    public string ParseString(ReadOnlySpan<char> value) => value.ToString().FromCsvField().ToString();

    public string UnescapeString(string value) => value.FromCsvField().ToString();

    public ReadOnlySpan<char> UnescapeString(ReadOnlySpan<char> value) => value.FromCsvField();

    public string EatTypeValue(string value, ref int i) => EatValue(value, ref i);

    public ReadOnlySpan<char> EatTypeValue(ReadOnlySpan<char> value, ref int i) => EatValue(value, ref i);

    public bool EatMapStartChar(string value, ref int i) => EatMapStartChar(value.AsSpan(), ref i);

    public bool EatMapStartChar(ReadOnlySpan<char> value, ref int i)
    {
        var success = value[i] == JsWriter.MapStartChar;
        if (success) i++;
        return success;
    }

    public string EatMapKey(string value, ref int i) => EatMapKey(value.AsSpan(), ref i).ToString();

    public ReadOnlySpan<char> EatMapKey(ReadOnlySpan<char> value, ref int i)
    {
        var tokenStartPos = i;

        var valueLength = value.Length;

        var valueChar = value[tokenStartPos];

        switch (valueChar)
        {
            case JsWriter.QuoteChar:
                while (++i < valueLength)
                {
                    valueChar = value[i];

                    if (valueChar != JsWriter.QuoteChar) continue;

                    var isLiteralQuote = i + 1 < valueLength && value[i + 1] == JsWriter.QuoteChar;

                    i++; //skip quote
                    if (!isLiteralQuote)
                        break;
                }
                var key = value.Slice(tokenStartPos, i - tokenStartPos);
                if (key.Length > 2 && key[0] == JsWriter.QuoteChar) // Don't unquote empty keys
                    key = key.Slice(1, key.Length - 2);
                return key;

            //Is Type/Map, i.e. {...}
            case JsWriter.MapStartChar:
                var endsToEat = 1;
                var withinQuotes = false;
                while (++i < valueLength && endsToEat > 0)
                {
                    valueChar = value[i];

                    if (valueChar == JsWriter.QuoteChar)
                        withinQuotes = !withinQuotes;

                    if (withinQuotes)
                        continue;

                    if (valueChar == JsWriter.MapStartChar)
                        endsToEat++;

                    if (valueChar == JsWriter.MapEndChar)
                        endsToEat--;
                }
                return value.Slice(tokenStartPos, i - tokenStartPos);
        }

        while (value[++i] != JsWriter.MapKeySeperator) { }
        return value.Slice(tokenStartPos, i - tokenStartPos);
    }

    public bool EatMapKeySeperator(string value, ref int i)
    {
        return value[i++] == JsWriter.MapKeySeperator;
    }

    public bool EatMapKeySeperator(ReadOnlySpan<char> value, ref int i)
    {
        return value[i++] == JsWriter.MapKeySeperator;
    }

    public bool EatItemSeperatorOrMapEndChar(string value, ref int i)
    {
        if (i == value.Length) return false;

        var success = value[i] == JsWriter.ItemSeperator
                      || value[i] == JsWriter.MapEndChar;

        if (success)
            i++;
        else if (Env.StrictMode) throw new Exception(
            $"Expected '{JsWriter.ItemSeperator}' or '{JsWriter.MapEndChar}'");
            
        return success;
    }

    public bool EatItemSeperatorOrMapEndChar(ReadOnlySpan<char> value, ref int i)
    {
        if (i == value.Length) return false;

        var success = value[i] == JsWriter.ItemSeperator
                      || value[i] == JsWriter.MapEndChar;

        if (success)
            i++;
        else if (Env.StrictMode) throw new Exception(
            $"Expected '{JsWriter.ItemSeperator}' or '{JsWriter.MapEndChar}'");
            
        return success;
    }

    public void EatWhitespace(string value, ref int i) {}

    public void EatWhitespace(ReadOnlySpan<char> value, ref int i) { }

    public string EatValue(string value, ref int i)
    {
        return EatValue(value.AsSpan(), ref i).ToString();
    }

    public ReadOnlySpan<char> EatValue(ReadOnlySpan<char> value, ref int i)
    {
        var tokenStartPos = i;
        var valueLength = value.Length;
        if (i == valueLength) return default;

        var valueChar = value[i];
        var withinQuotes = false;
        var endsToEat = 1;

        switch (valueChar)
        {
            //If we are at the end, return.
            case JsWriter.ItemSeperator:
            case JsWriter.MapEndChar:
                return default;

            //Is Within Quotes, i.e. "..."
            case JsWriter.QuoteChar:
                while (++i < valueLength)
                {
                    valueChar = value[i];

                    if (valueChar != JsWriter.QuoteChar) continue;

                    var isLiteralQuote = i + 1 < valueLength && value[i + 1] == JsWriter.QuoteChar;

                    i++; //skip quote
                    if (!isLiteralQuote)
                        break;
                }
                return value.Slice(tokenStartPos, i - tokenStartPos);

            //Is Type/Map, i.e. {...}
            case JsWriter.MapStartChar:
                while (++i < valueLength && endsToEat > 0)
                {
                    valueChar = value[i];

                    if (valueChar == JsWriter.QuoteChar)
                        withinQuotes = !withinQuotes;

                    if (withinQuotes)
                        continue;

                    if (valueChar == JsWriter.MapStartChar)
                        endsToEat++;

                    if (valueChar == JsWriter.MapEndChar)
                        endsToEat--;
                }
                return value.Slice(tokenStartPos, i - tokenStartPos);

            //Is List, i.e. [...]
            case JsWriter.ListStartChar:
                while (++i < valueLength && endsToEat > 0)
                {
                    valueChar = value[i];

                    if (valueChar == JsWriter.QuoteChar)
                        withinQuotes = !withinQuotes;

                    if (withinQuotes)
                        continue;

                    if (valueChar == JsWriter.ListStartChar)
                        endsToEat++;

                    if (valueChar == JsWriter.ListEndChar)
                        endsToEat--;
                }
                return value.Slice(tokenStartPos, i - tokenStartPos);
        }

        //Is Value
        while (++i < valueLength)
        {
            valueChar = value[i];

            if (valueChar == JsWriter.ItemSeperator
                || valueChar == JsWriter.MapEndChar)
            {
                break;
            }
        }

        return value.Slice(tokenStartPos, i - tokenStartPos);
    }
}