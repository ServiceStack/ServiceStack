//
// http://code.google.com/p/servicestack/wiki/TypeSerializer
// ServiceStack.Text: .NET C# POCO Type Text Serializer.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of ServiceStack: new BSD license.
//

using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Json
{
	public class JsonTypeSerializer
		: ITypeSerializer
	{
		public static ITypeSerializer Instance = new JsonTypeSerializer();

		public static readonly bool[] WhiteSpaceFlags = new bool[(int) ' ' + 1];

		static JsonTypeSerializer()
		{
			WhiteSpaceFlags[(int)' '] = true;
			WhiteSpaceFlags[(int)'\t'] = true;
			WhiteSpaceFlags[(int)'\r'] = true;
			WhiteSpaceFlags[(int)'\n'] = true;
		}

		public Action<TextWriter, object> GetWriteFn<T>()
		{
			return JsonWriter<T>.WriteFn();
		}

		public Action<TextWriter, object> GetWriteFn(Type type)
		{
			return JsonWriter.GetWriteFn(type);
		}

		/// <summary>
		/// Shortcut escape when we're sure value doesn't contain any escaped chars
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		public void WriteRawString(TextWriter writer, string value)
		{
			writer.Write('"');
			writer.Write(value);
			writer.Write('"');
		}

		public void WritePropertyName(TextWriter writer, string value)
		{
			WriteRawString(writer, value);
		}

		public void WriteString(TextWriter writer, string value)
		{
			JsonUtils.WriteString(writer, value);
		}

		public void WriteBuiltIn(TextWriter writer, object value)
		{
			WriteRawString(writer, value.ToString());
		}

		public void WriteObjectString(TextWriter writer, object value)
		{
			if (value != null)
			{
				WriteString(writer, value.ToString());
			}
		}

		public void WriteException(TextWriter writer, object value)
		{
			WriteString(writer, ((Exception)value).Message);
		}

		public void WriteDateTime(TextWriter writer, object oDateTime)
		{
			WriteRawString(writer, DateTimeSerializer.ToWcfJsonDate((DateTime)oDateTime));
		}

		public void WriteNullableDateTime(TextWriter writer, object dateTime)
		{
			if (dateTime == null) return;
			WriteDateTime(writer, dateTime);
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

		public void WriteInteger(TextWriter writer, object integerValue)
		{
			if (integerValue == null)
				writer.Write(JsonUtils.Null);
			else
				writer.Write(integerValue.ToString());
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
				writer.Write(((float)floatValue).ToString(CultureInfo.InvariantCulture));
		}

		public void WriteDouble(TextWriter writer, object doubleValue)
		{
			if (doubleValue == null)
				writer.Write(JsonUtils.Null);
			else
				writer.Write(((double)doubleValue).ToString(CultureInfo.InvariantCulture));
		}

		public void WriteDecimal(TextWriter writer, object decimalValue)
		{
			if (decimalValue == null)
				writer.Write(JsonUtils.Null);
			else
				writer.Write(((decimal)decimalValue).ToString(CultureInfo.InvariantCulture));
		}

		public object EncodeMapKey(object value)
		{
			var strValue = value.ToString();
			return strValue[0] != JsonUtils.QuoteChar ? '"' + strValue + '"' : value;
		}

		public Func<string, object> GetParseFn<T>()
		{
			return JsonReader.Instance.GetParseFn<T>();
		}

		public Func<string, object> GetParseFn(Type type)
		{
			return JsonReader.GetParseFn(type);
		}

		public string ParseRawString(string value)
		{
			if (string.IsNullOrEmpty(value)) return value;

			if (value[0] == JsonUtils.QuoteChar)
			{
				return value.Substring(1, value.Length - 2);
			}

			return value;
		}

		public string ParseString(string value)
		{
			if (string.IsNullOrEmpty(value)) return value;

			//var i = 0;
			//return ParseString(value, ref i);
			return ParseRawString(value);
		}

		private static string ParseString(string json, ref int index)
		{
			var jsonLength = json.Length;
			var sb = new StringBuilder(jsonLength);

			EatWhitespace(json, ref index);

			if (json[index] == JsonUtils.QuoteChar)
			{
				index++;

				//See if we can short-circuit evaluation (StringBuilder takes the most time here)
				var strEndPos = json.IndexOf(JsonUtils.QuoteChar, index);
				if (strEndPos == -1) strEndPos = jsonLength;
				var potentialValue = json.Substring(index, strEndPos - index);
				if (potentialValue.IndexOf(JsonUtils.EscapeChar) == -1)
				{
					index = strEndPos + 1;
					return potentialValue;
				}
			}

			char c;

			while (true)
			{
				if (index == jsonLength) break;

				c = json[index++];
				if (c == JsonUtils.QuoteChar) break;

				if (c == '\\')
				{

					if (index == jsonLength)
					{
						break;
					}
					c = json[index++];
					switch (c)
					{
						case '"':
							sb.Append('"');
							break;
						case '\\':
							sb.Append('\\');
							break;
						case '/':
							sb.Append('/');
							break;
						case 'b':
							sb.Append('\b');
							break;
						case 'f':
							sb.Append('\f');
							break;
						case 'n':
							sb.Append('\n');
							break;
						case 'r':
							sb.Append('\r');
							break;
						case 't':
							sb.Append('\t');
							break;
						case 'u':
							var remainingLength = jsonLength - index;
							if (remainingLength >= 4)
							{
								var unicodeCharArray = json.ToCharArray(index, 4);
								var unicodeIntVal = uint.Parse(new string(unicodeCharArray), NumberStyles.HexNumber);
								sb.Append(char.ConvertFromUtf32((int)unicodeIntVal));
								index += 4;
							}
							else
							{
								break;
							}
							break;
					}
				}
				else
				{
					sb.Append(c);
				}
			}

			var strValue = sb.ToString();
			return strValue == JsonUtils.Null ? null : strValue;
		}

		private static void EatWhitespace(string json, ref int index)
		{
			int c;
			for (; index < json.Length; index++)
			{
				c = json[index];
				if (c >= WhiteSpaceFlags.Length || !WhiteSpaceFlags[c])
				{
					break;
				}
			}
		}

		public string EatTypeValue(string value, ref int i)
		{
			return EatMapValue(value, ref i);
		}

		public static string EatUntilCharFound(string value, ref int i, char findChar)
		{
			var tokenStartPos = i;
			var valueLength = value.Length;
			if (value[tokenStartPos] != JsWriter.QuoteChar)
			{
				i = value.IndexOf(findChar, tokenStartPos);
				if (i == -1) i = valueLength;
				return value.Substring(tokenStartPos, i - tokenStartPos);
			}

			while (++i < valueLength)
			{
				if (value[i] == JsWriter.QuoteChar)
				{
					//if we reach the end return
					if (i + 1 >= valueLength)
					{
						return value.Substring(tokenStartPos, ++i - tokenStartPos);
					}

					//skip past 'escaped quotes'
					if (value[i + 1] == JsonUtils.EscapeChar
						&& i + 2 < valueLength
						&& value[i + 2] == JsonUtils.QuoteChar)
					{
						i += 2;
					}
					else if (value[i + 1] == findChar)
					{
						return value.Substring(tokenStartPos, ++i - tokenStartPos);
					}
				}
			}

			throw new IndexOutOfRangeException("Could not find ending quote");
		}

		public bool EatMapStartChar(string value, ref int i)
		{
			EatWhitespace(value, ref i);
			return value[i++] == JsWriter.MapStartChar;
		}

		public string EatElementValue(string value, ref int i)
		{
			return EatUntilCharFound(value, ref i, JsWriter.ItemSeperator);
		}

		public string EatMapKey(string value, ref int i)
		{
			return ParseString(value, ref i);
		}

		public bool EatMapKeySeperator(string value, ref int i)
		{
			EatWhitespace(value, ref i);
			return value[i++] == JsWriter.MapKeySeperator;
		}

		public bool EatMapItemSeperatorOrEndChar(string value, ref int i)
		{
			EatWhitespace(value, ref i);
			
			var success = value[i] == JsWriter.ItemSeperator 
				|| value[i] == JsWriter.MapEndChar;
			
			i++;

			if (success)
			{
				EatWhitespace(value, ref i);
			}

			return success;
		}

		public string EatMapValue(string value, ref int i)
		{
			var valueLength = value.Length;
			if (i == valueLength) return null;

			EatWhitespace(value, ref i);

			var tokenStartPos = i;
			var valueChar = value[i];

			//If we are at the end, return.
			if (valueChar == JsWriter.ItemSeperator
				|| valueChar == JsWriter.MapEndChar)
			{
				return null;
			}

			//Is List, i.e. [...]
			var withinQuotes = false;
			if (valueChar == JsWriter.ListStartChar)
			{
				var endsToEat = 1;
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
				return value.Substring(tokenStartPos, i - tokenStartPos);
			}

			//Is Type/Map, i.e. {...}
			if (valueChar == JsWriter.MapStartChar)
			{
				var endsToEat = 1;
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
				return value.Substring(tokenStartPos, i - tokenStartPos);
			}

			//Is Within Quotes, i.e. "..."
			if (valueChar == JsWriter.QuoteChar)
			{
				return ParseString(value, ref i);
			}

			//Is Value
			while (++i < valueLength)
			{
				valueChar = value[i];

				if (valueChar == JsWriter.ItemSeperator
					|| valueChar == JsWriter.MapEndChar
					//If it doesn't have quotes it's either a keyword or number so also has a ws boundary
					|| (valueChar < WhiteSpaceFlags.Length && WhiteSpaceFlags[valueChar])
				)
				{
					break;
				}
			}

			var strValue = value.Substring(tokenStartPos, i - tokenStartPos);
			return strValue == JsonUtils.Null ? null : strValue;
		}
	}

}