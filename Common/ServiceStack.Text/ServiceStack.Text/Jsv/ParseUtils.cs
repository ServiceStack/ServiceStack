using System;
using System.Collections.Generic;

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
			return EatUntilCharFound(value, ref i, TypeSerializer.ItemSeperator);
		}

		private static string EatUntilCharFound(string value, ref int i, char findChar)
		{
			var tokenStartPos = i;
			var valueLength = value.Length;
			if (value[tokenStartPos] != TypeSerializer.QuoteChar)
			{
				i = value.IndexOf(findChar, tokenStartPos);
				if (i == -1) i = valueLength;
				return value.Substring(tokenStartPos, i - tokenStartPos);
			}

			while (++i < valueLength)
			{
				if (value[i] == TypeSerializer.QuoteChar
				    && (i + 1 >= valueLength || value[i + 1] == findChar))
				{
					i++;
					return value.Substring(tokenStartPos, i - tokenStartPos);
				}
			}

			throw new IndexOutOfRangeException("Could not find ending quote");
		}

		public static string EatMapKey(string value, ref int i)
		{
			var tokenStartPos = i;
			while (value[++i] != TypeSerializer.MapKeySeperator) { }
			return value.Substring(tokenStartPos, i - tokenStartPos);
		}

		public static string EatMapValue(string value, ref int i)
		{
			var tokenStartPos = i;
			var valueLength = value.Length;
			var valueChar = value[i];

			//If we are at the end, return.
			if (i == valueLength
				|| valueChar == TypeSerializer.ItemSeperator
				|| valueChar == TypeSerializer.MapEndChar)
			{
				return null;
			}

			//Is List, i.e. [...]
			var withinQuotes = false;
			if (valueChar == TypeSerializer.ListStartChar)
			{
				var endsToEat = 1;
				while (++i < valueLength && endsToEat > 0)
				{
					valueChar = value[i];

					if (valueChar == TypeSerializer.QuoteChar)
						withinQuotes = !withinQuotes;

					if (withinQuotes)
						continue;

					if (valueChar == TypeSerializer.ListStartChar)
						endsToEat++;

					if (valueChar == TypeSerializer.ListEndChar)
						endsToEat--;
				}
				return value.Substring(tokenStartPos, i - tokenStartPos);
			}

			//Is Type/Map, i.e. {...}
			if (valueChar == TypeSerializer.MapStartChar)
			{
				var endsToEat = 1;
				while (++i < valueLength && endsToEat > 0)
				{
					valueChar = value[i];

					if (valueChar == TypeSerializer.QuoteChar)
						withinQuotes = !withinQuotes;

					if (withinQuotes)
						continue;

					if (valueChar == TypeSerializer.MapStartChar)
						endsToEat++;

					if (valueChar == TypeSerializer.MapEndChar)
						endsToEat--;
				}
				return value.Substring(tokenStartPos, i - tokenStartPos);
			}


			//Is Within Quotes, i.e. "..."
			if (valueChar == TypeSerializer.QuoteChar)
			{
				while (++i < valueLength)
				{
					valueChar = value[i];

					if (valueChar != TypeSerializer.QuoteChar) continue;

					var isLiteralQuote = i + 1 < valueLength && value[i + 1] == TypeSerializer.QuoteChar;

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

				if (valueChar == TypeSerializer.ItemSeperator
					|| valueChar == TypeSerializer.MapEndChar)
				{
					break;
				}
			}

			return value.Substring(tokenStartPos, i - tokenStartPos);
		}

	}
}