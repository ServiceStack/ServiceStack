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
using System.Collections.Generic;
using System.Reflection;
using ServiceStack.Text.Common;

namespace ServiceStack.Text.Jsv
{
	public static class ParseUtils
	{
		public static object NullValueType(Type type)
		{
			return ReflectionExtensions.GetDefaultValue(type);
		}

		public static string ParseString(string value)
		{
			return value.FromCsvField();
		}

		public static object ParseObject(string value)
		{
			return value;
		}

		public static object ParseEnum(Type type, string value)
		{
			return Enum.Parse(type, value);
		}

		public static string EatTypeValue(string value, ref int i)
		{
			return EatMapValue(value, ref i);
		}

		public static string EatElementValue(string value, ref int i)
		{
			return EatUntilCharFound(value, ref i, JsWriter.ItemSeperator);
		}

		private static string EatUntilCharFound(string value, ref int i, char findChar)
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

		public static string EatMapKey(string value, ref int i)
		{
			var tokenStartPos = i;
			while (value[++i] != JsWriter.MapKeySeperator) { }
			return value.Substring(tokenStartPos, i - tokenStartPos);
		}

		public static string EatMapValue(string value, ref int i)
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