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
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Jsv
{
	public class JsvTypeSerializer 
		: ITypeSerializer
	{
		public static ITypeSerializer Instance = new JsvTypeSerializer();

		public Action<TextWriter, object> GetWriteFn<T>()
		{
			return JsvWriter<T>.WriteFn();
		}

		public Action<TextWriter, object> GetWriteFn(Type type)
		{
			return JsvWriter.GetWriteFn(type);
		}

		public void WriteRawString(TextWriter writer, string value)
		{
			writer.Write(value.ToCsvField());
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
				writer.Write(value.ToString().ToCsvField());
			}
		}

		public void WriteException(TextWriter writer, object value)
		{
			writer.Write(((Exception)value).Message.ToCsvField());
		}

		public void WriteString(TextWriter writer, string value)
		{
			writer.Write(value.ToCsvField());
		}

		public void WriteDateTime(TextWriter writer, object oDateTime)
		{
			writer.Write(DateTimeSerializer.ToShortestXsdDateTimeString((DateTime)oDateTime));
		}

		public void WriteNullableDateTime(TextWriter writer, object dateTime)
		{
			if (dateTime == null) return;
			writer.Write(DateTimeSerializer.ToShortestXsdDateTimeString((DateTime)dateTime));
		}

		public void WriteGuid(TextWriter writer, object oValue)
		{
			writer.Write(((Guid)oValue).ToString("N"));
		}

		public void WriteNullableGuid(TextWriter writer, object oValue)
		{
			if (oValue == null) return;
			writer.Write(((Guid)oValue).ToString("N"));
		}

		public void WriteBytes(TextWriter writer, object oByteValue)
		{
			if (oByteValue == null) return;
			writer.Write(Convert.ToBase64String((byte[])oByteValue));
		}

		public void WriteInteger(TextWriter writer, object integerValue)
		{
			if (integerValue == null) return;
			writer.Write(integerValue.ToString());
		}

		public void WriteBool(TextWriter writer, object boolValue)
		{
			if (boolValue == null) return;
			writer.Write(boolValue.ToString());
		}

		public void WriteFloat(TextWriter writer, object floatValue)
		{
			if (floatValue == null) return;
			writer.Write(((float)floatValue).ToString(CultureInfo.InvariantCulture));
		}

		public void WriteDouble(TextWriter writer, object doubleValue)
		{
			if (doubleValue == null) return;
			writer.Write(((double)doubleValue).ToString(CultureInfo.InvariantCulture));
		}

		public void WriteDecimal(TextWriter writer, object decimalValue)
		{
			if (decimalValue == null) return;
			writer.Write(((decimal)decimalValue).ToString(CultureInfo.InvariantCulture));
		}


		public object EncodeMapKey(object value)
		{
			return value;
		}

		public Func<string, object> GetParseFn<T>()
		{
			return JsvReader.Instance.GetParseFn<T>();
		}

		public Func<string, object> GetParseFn(Type type)
		{
			return JsvReader.GetParseFn(type);
		}

		public string ParseRawString(string value)
		{
			return value;
		}

		public string ParseString(string value)
		{
			return value.FromCsvField();
		}

		public string EatTypeValue(string value, ref int i)
		{
			return EatMapValue(value, ref i);
		}

		public string EatElementValue(string value, ref int i)
		{
			return EatUntilCharFound(value, ref i, JsWriter.ItemSeperator);
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
					if (value[i + 1] == JsWriter.QuoteChar)
					{
						i++;
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
			return value[i++] == JsWriter.MapStartChar;
		}

		public string EatMapKey(string value, ref int i)
		{
			var tokenStartPos = i;
			while (value[++i] != JsWriter.MapKeySeperator) { }
			return value.Substring(tokenStartPos, i - tokenStartPos);
		}

		public bool EatMapKeySeperator(string value, ref int i)
		{
			return value[i++] == JsWriter.MapKeySeperator;
		}

		public bool EatMapItemSeperatorOrEndChar(string value, ref int i)
		{
			var success = value[i] == JsWriter.ItemSeperator
				|| value[i] == JsWriter.MapEndChar;
			i++;
			return success;
		}

		public string EatMapValue(string value, ref int i)
		{
			var tokenStartPos = i;
			var valueLength = value.Length;
			if (i == valueLength) return null;

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
				while (++i < valueLength)
				{
					valueChar = value[i];

					if (valueChar != JsWriter.QuoteChar) continue;

					var isLiteralQuote = i + 1 < valueLength && value[i + 1] == JsWriter.QuoteChar;

					i++; //skip quote
					if (!isLiteralQuote)
						break;
				}
				return value.Substring(tokenStartPos, i - tokenStartPos);
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

			return value.Substring(tokenStartPos, i - tokenStartPos);
		}
	}
}