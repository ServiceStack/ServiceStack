//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Json
{
    public struct JsonTypeSerializer
        : ITypeSerializer
    {
        public static ITypeSerializer Instance = new JsonTypeSerializer();

        public ObjectDeserializerDelegate ObjectDeserializer { get; set; }

        public bool IncludeNullValues => JsConfig.IncludeNullValues;

        public bool IncludeNullValuesInDictionaries => JsConfig.IncludeNullValuesInDictionaries;

        public string TypeAttrInObject => JsConfig.JsonTypeAttrInObject;

        internal static string GetTypeAttrInObject(string typeAttr) => $"{{\"{typeAttr}\":";

        public WriteObjectDelegate GetWriteFn<T>() => JsonWriter<T>.WriteFn();

        public WriteObjectDelegate GetWriteFn(Type type) => JsonWriter.GetWriteFn(type);

        public TypeInfo GetTypeInfo(Type type) => JsonWriter.GetTypeInfo(type);

        /// <summary>
        /// Shortcut escape when we're sure value doesn't contain any escaped chars
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="value"></param>
        public void WriteRawString(TextWriter writer, string value)
        {
            writer.Write(JsWriter.QuoteChar);
            writer.Write(value);
            writer.Write(JsWriter.QuoteChar);
        }

        public void WritePropertyName(TextWriter writer, string value)
        {
            if (JsState.WritingKeyCount > 0)
            {
                writer.Write(JsWriter.EscapedQuoteString);
                writer.Write(value);
                writer.Write(JsWriter.EscapedQuoteString);
            }
            else
            {
                WriteRawString(writer, value);
            }
        }

        public void WriteString(TextWriter writer, string value)
        {
            JsonUtils.WriteString(writer, value);
        }

        public void WriteBuiltIn(TextWriter writer, object value)
        {
            if (JsState.WritingKeyCount > 0 && !JsState.IsWritingValue) writer.Write(JsonUtils.QuoteChar);

            WriteRawString(writer, value.ToString());

            if (JsState.WritingKeyCount > 0 && !JsState.IsWritingValue) writer.Write(JsonUtils.QuoteChar);
        }

        public void WriteObjectString(TextWriter writer, object value)
        {
            JsonUtils.WriteString(writer, value?.ToString());
        }

        public void WriteFormattableObjectString(TextWriter writer, object value)
        {
            var formattable = value as IFormattable;
            JsonUtils.WriteString(writer, formattable?.ToString(null, CultureInfo.InvariantCulture));
        }

        public void WriteException(TextWriter writer, object value)
        {
            WriteString(writer, ((Exception)value).Message);
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

            writer.Write(JsWriter.QuoteString);
            DateTimeSerializer.WriteWcfJsonDate(writer, dateTime);
            writer.Write(JsWriter.QuoteString);
        }

        public void WriteNullableDateTime(TextWriter writer, object dateTime)
        {
            if (dateTime == null)
                writer.Write(JsonUtils.Null);
            else
                WriteDateTime(writer, dateTime);
        }

        public void WriteDateTimeOffset(TextWriter writer, object oDateTimeOffset)
        {
            writer.Write(JsWriter.QuoteString);
            DateTimeSerializer.WriteWcfJsonDateTimeOffset(writer, (DateTimeOffset)oDateTimeOffset);
            writer.Write(JsWriter.QuoteString);
        }

        public void WriteNullableDateTimeOffset(TextWriter writer, object dateTimeOffset)
        {
            if (dateTimeOffset == null)
                writer.Write(JsonUtils.Null);
            else
                WriteDateTimeOffset(writer, dateTimeOffset);
        }

        public void WriteTimeSpan(TextWriter writer, object oTimeSpan)
        {
            var stringValue = JsConfig.TimeSpanHandler == TimeSpanHandler.StandardFormat
                ? oTimeSpan.ToString()
                : DateTimeSerializer.ToXsdTimeSpanString((TimeSpan)oTimeSpan);
            WriteRawString(writer, stringValue);
        }

        public void WriteNullableTimeSpan(TextWriter writer, object oTimeSpan)
        {
            if (oTimeSpan == null) return;
            WriteTimeSpan(writer, ((TimeSpan?)oTimeSpan).Value);
        }

        public void WriteGuid(TextWriter writer, object oValue)
        {
            WriteRawString(writer, ((Guid)oValue).ToString("N"));
        }

        public void WriteNullableGuid(TextWriter writer, object oValue)
        {
            if (oValue == null) return;
            WriteRawString(writer, ((Guid)oValue).ToString("N"));
        }

        public void WriteBytes(TextWriter writer, object oByteValue)
        {
            if (oByteValue == null) return;
            WriteRawString(writer, Convert.ToBase64String((byte[])oByteValue));
        }

        public void WriteChar(TextWriter writer, object charValue)
        {
            if (charValue == null)
                writer.Write(JsonUtils.Null);
            else
                WriteString(writer, ((char)charValue).ToString());
        }

        public void WriteByte(TextWriter writer, object byteValue)
        {
            if (byteValue == null)
                writer.Write(JsonUtils.Null);
            else
                writer.Write((byte)byteValue);
        }

        public void WriteSByte(TextWriter writer, object sbyteValue)
        {
            if (sbyteValue == null)
                writer.Write(JsonUtils.Null);
            else
                writer.Write((sbyte)sbyteValue);
        }

        public void WriteInt16(TextWriter writer, object intValue)
        {
            if (intValue == null)
                writer.Write(JsonUtils.Null);
            else
                writer.Write((short)intValue);
        }

        public void WriteUInt16(TextWriter writer, object intValue)
        {
            if (intValue == null)
                writer.Write(JsonUtils.Null);
            else
                writer.Write((ushort)intValue);
        }

        public void WriteInt32(TextWriter writer, object intValue)
        {
            if (intValue == null)
                writer.Write(JsonUtils.Null);
            else
                writer.Write((int)intValue);
        }

        public void WriteUInt32(TextWriter writer, object uintValue)
        {
            if (uintValue == null)
                writer.Write(JsonUtils.Null);
            else
                writer.Write((uint)uintValue);
        }

        public void WriteInt64(TextWriter writer, object integerValue)
        {
            if (integerValue == null)
                writer.Write(JsonUtils.Null);
            else
                writer.Write((long)integerValue);
        }

        public void WriteUInt64(TextWriter writer, object ulongValue)
        {
            if (ulongValue == null)
            {
                writer.Write(JsonUtils.Null);
            }
            else
                writer.Write((ulong)ulongValue);
        }

        public void WriteBool(TextWriter writer, object boolValue)
        {
            if (boolValue == null)
                writer.Write(JsonUtils.Null);
            else
                writer.Write(((bool)boolValue) ? JsonUtils.True : JsonUtils.False);
        }

        public void WriteFloat(TextWriter writer, object floatValue)
        {
            if (floatValue == null)
                writer.Write(JsonUtils.Null);
            else
            {
                var floatVal = (float)floatValue;
                if (Equals(floatVal, float.MaxValue) || Equals(floatVal, float.MinValue))
                    writer.Write(floatVal.ToString("r", CultureInfo.InvariantCulture));
                else
                    writer.Write(floatVal.ToString("r", CultureInfo.InvariantCulture));
            }
        }

        public void WriteDouble(TextWriter writer, object doubleValue)
        {
            if (doubleValue == null)
                writer.Write(JsonUtils.Null);
            else
            {
                var doubleVal = (double)doubleValue;
                if (Equals(doubleVal, double.MaxValue) || Equals(doubleVal, double.MinValue))
                    writer.Write(doubleVal.ToString("r", CultureInfo.InvariantCulture));
                else
                    writer.Write(doubleVal.ToString(CultureInfo.InvariantCulture));
            }
        }

        public void WriteDecimal(TextWriter writer, object decimalValue)
        {
            if (decimalValue == null)
                writer.Write(JsonUtils.Null);
            else
                writer.Write(((decimal)decimalValue).ToString(CultureInfo.InvariantCulture));
        }

        public void WriteEnum(TextWriter writer, object enumValue)
        {
            if (enumValue == null) 
                return;
            var serializedValue = CachedTypeInfo.Get(enumValue.GetType()).EnumInfo.GetSerializedValue(enumValue);
            if (serializedValue is string strEnum)
                WriteRawString(writer, strEnum);
            else
                JsWriter.WriteEnumFlags(writer, enumValue);
        }


#if NET6_0
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
                    writer.Write(JsWriter.QuoteString);
                    writer.Write(dateOnly.ToString("O"));
                    writer.Write(JsWriter.QuoteString);
                    break;
            }
        }

        public void WriteNullableDateOnly(TextWriter writer, object oDateOnly)
        {
            if (oDateOnly == null)
                writer.Write(JsonUtils.Null);
            else
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseStringDelegate GetParseFn<T>()
        {
            return JsonReader.Instance.GetParseFn<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseStringSpanDelegate GetParseStringSpanFn<T>()
        {
            return JsonReader.Instance.GetParseStringSpanFn<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseStringDelegate GetParseFn(Type type)
        {
            return JsonReader.GetParseFn(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ParseStringSpanDelegate GetParseStringSpanFn(Type type)
        {
            return JsonReader.GetParseStringSpanFn(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ParseRawString(string value)
        {
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ParseString(ReadOnlySpan<char> value)
        {
            return value.IsNullOrEmpty() ? null : ParseRawString(value.ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ParseString(string value)
        {
            return string.IsNullOrEmpty(value) ? value : ParseRawString(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmptyMap(ReadOnlySpan<char> value, int i = 1)
        {
            for (; i < value.Length; i++) { var c = value[i]; if (!JsonUtils.IsWhiteSpace(c)) break; } //Whitespace inline
            if (value.Length == i) return true;
            return value[i++] == JsWriter.MapEndChar;
        }

        internal static ReadOnlySpan<char> ParseString(ReadOnlySpan<char> json, ref int index)
        {
            var jsonLength = json.Length;

            if (json[index] != JsonUtils.QuoteChar)
                throw new Exception("Invalid unquoted string starting with: " + json.SafeSubstring(50).ToString());

            var startIndex = ++index;
            do
            {
                var c = json[index];

                if (c == JsonUtils.QuoteChar) 
                    break;
                
                if (c == JsonUtils.EscapeChar)
                {
                    index++;
                    if (json[index] == 'u')
                        index += 4;
                }
                
            } while (index++ < jsonLength);

            if (index == jsonLength)
                throw new Exception("Invalid unquoted string ending with: " + json.SafeSubstring(json.Length - 50, 50).ToString());
            
            index++;
            var str = json.Slice(startIndex, Math.Min(index, jsonLength) - startIndex - 1);
            if (str.Length == 0)
                return TypeConstants.EmptyStringSpan; 
                    
            return str;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string UnescapeString(string value)
        {
            var i = 0;
            return UnescapeJsonString(value, ref i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> UnescapeString(ReadOnlySpan<char> value)
        {
            var i = 0;
            return UnescapeJsonString(value, ref i);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public object UnescapeStringAsObject(ReadOnlySpan<char> value)
        {
            var ignore = 0;
            return UnescapeJsString(value, JsonUtils.QuoteChar, removeQuotes: true, ref ignore).Value();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string UnescapeSafeString(string value) => UnescapeSafeString(value.AsSpan()).ToString();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> UnescapeSafeString(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty) 
                return value;

            if (value[0] == JsonUtils.QuoteChar && value[value.Length - 1] == JsonUtils.QuoteChar)
                return value.Slice(1, value.Length - 2);
            
            return value;
        }

        static readonly char[] IsSafeJsonChars = { JsonUtils.QuoteChar, JsonUtils.EscapeChar };

        internal static ReadOnlySpan<char> ParseJsonString(ReadOnlySpan<char> json, ref int index)
        {
            for (; index < json.Length; index++) { var ch = json[index]; if (!JsonUtils.IsWhiteSpace(ch)) break; } //Whitespace inline

            return UnescapeJsonString(json, ref index);
        }

        private static string UnescapeJsonString(string json, ref int index)
        {
            return json != null 
                ? UnescapeJsonString(json.AsSpan(), ref index).ToString() 
                : null;
        }

        private static ReadOnlySpan<char> UnescapeJsonString(ReadOnlySpan<char> json, ref int index) =>
            UnescapeJsString(json, JsonUtils.QuoteChar, removeQuotes:true, ref index);

        public static ReadOnlySpan<char> UnescapeJsString(ReadOnlySpan<char> json, char quoteChar)
        {
            var ignore = 0;
            return UnescapeJsString(json, quoteChar, removeQuotes:false, ref ignore);
        }
        
        public static ReadOnlySpan<char> UnescapeJsString(ReadOnlySpan<char> json, char quoteChar, bool removeQuotes, ref int index)
        {
            if (json.IsNullOrEmpty()) return json;
            var jsonLength = json.Length;
            var buffer = json;

            var firstChar = buffer[index];
            if (firstChar == quoteChar)
            {
                index++;

                //MicroOp: See if we can short-circuit evaluation (to avoid StringBuilder)
                var jsonAtIndex = json.Slice(index);
                var strEndPos = jsonAtIndex.IndexOfAny(IsSafeJsonChars);
                if (strEndPos == -1) 
                    return jsonAtIndex.Slice(0, jsonLength);

                if (jsonAtIndex[strEndPos] == quoteChar)
                {
                    var potentialValue = jsonAtIndex.Slice(0, strEndPos);
                    index += strEndPos + 1;
                    return potentialValue.Length > 0
                        ? potentialValue
                        : TypeConstants.EmptyStringSpan;
                }
            }
            else
            {
                var i = index;
                var end = jsonLength;

                while (i < end)
                {
                    var c = buffer[i];
                    if (c == quoteChar || c == JsonUtils.EscapeChar)
                        break;
                    i++;
                }
                if (i == end) 
                    return buffer.Slice(index, jsonLength - index);
            }

            return Unescape(json, removeQuotes:removeQuotes, quoteChar:quoteChar);
        }
        
        public static string Unescape(string input) => Unescape(input, true);
        public static string Unescape(string input, bool removeQuotes) => Unescape(input.AsSpan(), removeQuotes).ToString();

        public static ReadOnlySpan<char> Unescape(ReadOnlySpan<char> input) => Unescape(input, true);

        public static ReadOnlySpan<char> Unescape(ReadOnlySpan<char> input, bool removeQuotes) =>
            Unescape(input, removeQuotes, JsonUtils.QuoteChar);

        public static ReadOnlySpan<char> Unescape(ReadOnlySpan<char> input, bool removeQuotes, char quoteChar)
        {
            var length = input.Length;
            int start = 0;
            int count = 0;
            var output = StringBuilderThreadStatic.Allocate();
            for (; count < length;)
            {
                var c = input[count];
                if (removeQuotes)
                {
                    if (c == quoteChar)
                    {
                        if (start != count)
                        {
                            output.Append(input.Slice(start, count - start));
                        }
                        count++;
                        start = count;
                        continue;
                    }
                }

                if (c == JsonUtils.EscapeChar)
                {
                    if (start != count)
                    {
                        output.Append(input.Slice(start, count - start));
                    }
                    start = count;
                    count++;
                    if (count >= length) continue;

                    //we will always be parsing an escaped char here
                    c = input[count];

                    switch (c)
                    {
                        case 'a':
                            output.Append('\a');
                            count++;
                            break;
                        case 'b':
                            output.Append('\b');
                            count++;
                            break;
                        case 'f':
                            output.Append('\f');
                            count++;
                            break;
                        case 'n':
                            output.Append('\n');
                            count++;
                            break;
                        case 'r':
                            output.Append('\r');
                            count++;
                            break;
                        case 'v':
                            output.Append('\v');
                            count++;
                            break;
                        case 't':
                            output.Append('\t');
                            count++;
                            break;
                        case 'u':
                            if (count + 4 < length)
                            {
                                var unicodeString = input.Slice(count + 1, 4);
                                var unicodeIntVal = MemoryProvider.Instance.ParseUInt32(unicodeString, NumberStyles.HexNumber);
                                output.Append(ConvertFromUtf32((int)unicodeIntVal));
                                count += 5;
                            }
                            else
                            {
                                output.Append(c);
                            }
                            break;
                        case 'x':
                            if (count + 4 < length)
                            {
                                var unicodeString = input.Slice(count + 1, 4);
                                var unicodeIntVal = MemoryProvider.Instance.ParseUInt32(unicodeString, NumberStyles.HexNumber);
                                output.Append(ConvertFromUtf32((int)unicodeIntVal));
                                count += 5;
                            }
                            else
                            if (count + 2 < length)
                            {
                                var unicodeString = input.Slice(count + 1, 2);
                                var unicodeIntVal = MemoryProvider.Instance.ParseUInt32(unicodeString, NumberStyles.HexNumber);
                                output.Append(ConvertFromUtf32((int)unicodeIntVal));
                                count += 3;
                            }
                            else
                            {
                                output.Append(input.Slice(start, count - start));
                            }
                            break;
                        default:
                            output.Append(c);
                            count++;
                            break;
                    }
                    start = count;
                }
                else
                {
                    count++;
                }
            }
            output.Append(input.Slice(start, length - start));
            return StringBuilderThreadStatic.ReturnAndFree(output).AsSpan();
        }

        /// <summary>
        /// Given a character as utf32, returns the equivalent string provided that the character
        /// is legal json.
        /// </summary>
        /// <param name="utf32"></param>
        /// <returns></returns>
        public static string ConvertFromUtf32(int utf32)
        {
            if (utf32 < 0 || utf32 > 0x10FFFF)
                throw new ArgumentOutOfRangeException(nameof(utf32), "The argument must be from 0 to 0x10FFFF.");
            if (utf32 < 0x10000)
                return new string((char)utf32, 1);
            utf32 -= 0x10000;
            return new string(new[] {(char) ((utf32 >> 10) + 0xD800), (char) (utf32 % 0x0400 + 0xDC00)});
        }

        public string EatTypeValue(string value, ref int i)
        {
            return EatValue(value, ref i);
        }

        public ReadOnlySpan<char> EatTypeValue(ReadOnlySpan<char> value, ref int i)
        {
            return EatValue(value, ref i);
        }

        public bool EatMapStartChar(string value, ref int i) => EatMapStartChar(value.AsSpan(), ref i);

        public bool EatMapStartChar(ReadOnlySpan<char> value, ref int i)
        {
            for (; i < value.Length; i++) { var c = value[i]; if (!JsonUtils.IsWhiteSpace(c)) break; } //Whitespace inline
            return value[i++] == JsWriter.MapStartChar;
        }

        public string EatMapKey(string value, ref int i) => EatMapKey(value.AsSpan(), ref i).ToString();

        public ReadOnlySpan<char> EatMapKey(ReadOnlySpan<char> value, ref int i)
        {
            var valueLength = value.Length;
            for (; i < value.Length; i++) { var c = value[i]; if (!JsonUtils.IsWhiteSpace(c)) break; } //Whitespace inline

            var tokenStartPos = i;
            var valueChar = value[i];

            switch (valueChar)
            {
                //If we are at the end, return.
                case JsWriter.ItemSeperator:
                case JsWriter.MapEndChar:
                    return default(ReadOnlySpan<char>);

                //Is Within Quotes, i.e. "..."
                case JsWriter.QuoteChar:
                    return ParseString(value, ref i);
            }

            //Is Value
            while (++i < valueLength)
            {
                valueChar = value[i];

                if (valueChar == JsWriter.ItemSeperator
                    //If it doesn't have quotes it's either a keyword or number so also has a ws boundary
                    || (JsonUtils.IsWhiteSpace(valueChar))
                )
                {
                    break;
                }
            }

            return value.Slice(tokenStartPos, i - tokenStartPos);
        }

        public bool EatMapKeySeperator(string value, ref int i) => EatMapKeySeperator(value.AsSpan(), ref i);


        public bool EatMapKeySeperator(ReadOnlySpan<char> value, ref int i)
        {
            for (; i < value.Length; i++) { var c = value[i]; if (!JsonUtils.IsWhiteSpace(c)) break; } //Whitespace inline
            if (value.Length == i) return false;
            return value[i++] == JsWriter.MapKeySeperator;
        }

        public bool EatItemSeperatorOrMapEndChar(string value, ref int i)
        {
            return EatItemSeperatorOrMapEndChar(value.AsSpan(), ref i);
        }

        public bool EatItemSeperatorOrMapEndChar(ReadOnlySpan<char> value, ref int i)
        {
            for (; i < value.Length; i++) { var c = value[i]; if (!JsonUtils.IsWhiteSpace(c)) break; } //Whitespace inline

            if (i == value.Length) return false;

            var success = value[i] == JsWriter.ItemSeperator || value[i] == JsWriter.MapEndChar;

            if (success)
            {
                i++;

                for (; i < value.Length; i++) { var c = value[i]; if (!JsonUtils.IsWhiteSpace(c)) break; } //Whitespace inline
            }
            else if (Env.StrictMode) throw new Exception(
                $"Expected '{JsWriter.ItemSeperator}' or '{JsWriter.MapEndChar}'");

            return success;
        }

        public void EatWhitespace(ReadOnlySpan<char> value, ref int i)
        {
            for (; i < value.Length; i++) { var c = value[i]; if (!JsonUtils.IsWhiteSpace(c)) break; } //Whitespace inline
        }

        public void EatWhitespace(string value, ref int i)
        {
            for (; i < value.Length; i++) { var c = value[i]; if (!JsonUtils.IsWhiteSpace(c)) break; } //Whitespace inline
        }

        public string EatValue(string value, ref int i)
        {
            return EatValue(value.AsSpan(), ref i).ToString();
        }

        public ReadOnlySpan<char> EatValue(ReadOnlySpan<char> value, ref int i)
        {
            var buf = value;
            var valueLength = value.Length;
            if (i == valueLength) return default;

            while (i < valueLength && JsonUtils.IsWhiteSpace(buf[i])) i++; //Whitespace inline
            if (i == valueLength) return default;

            var tokenStartPos = i;
            var valueChar = buf[i];
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
                    return ParseString(value, ref i);

                //Is Type/Map, i.e. {...}
                case JsWriter.MapStartChar:
                    while (++i < valueLength)
                    {
                        valueChar = buf[i];

                        if (valueChar == JsonUtils.EscapeChar)
                        {
                            i++;
                            continue;
                        }

                        if (valueChar == JsWriter.QuoteChar)
                            withinQuotes = !withinQuotes;

                        if (withinQuotes)
                            continue;

                        if (valueChar == JsWriter.MapStartChar)
                            endsToEat++;

                        if (valueChar == JsWriter.MapEndChar && --endsToEat == 0)
                        {
                            i++;
                            break;
                        }
                    }
                    return value.Slice(tokenStartPos, i - tokenStartPos);

                //Is List, i.e. [...]
                case JsWriter.ListStartChar:
                    while (++i < valueLength)
                    {
                        valueChar = buf[i];

                        if (valueChar == JsonUtils.EscapeChar)
                        {
                            i++;
                            continue;
                        }

                        if (valueChar == JsWriter.QuoteChar)
                            withinQuotes = !withinQuotes;

                        if (withinQuotes)
                            continue;

                        if (valueChar == JsWriter.ListStartChar)
                            endsToEat++;

                        if (valueChar == JsWriter.ListEndChar && --endsToEat == 0)
                        {
                            i++;
                            break;
                        }
                    }
                    return value.Slice(tokenStartPos, i - tokenStartPos);
            }

            //Is Value
            while (++i < valueLength)
            {
                valueChar = buf[i];

                if (valueChar == JsWriter.ItemSeperator
                    || valueChar == JsWriter.MapEndChar
                    //If it doesn't have quotes it's either a keyword or number so also has a ws boundary
                    || JsonUtils.IsWhiteSpace(valueChar)
                )
                {
                    break;
                }
            }

            var strValue = value.Slice(tokenStartPos, i - tokenStartPos);
            
            return strValue.Equals(JsonUtils.Null.AsSpan(), StringComparison.Ordinal) 
                ? default 
                : strValue;
        }
    }

}