using System;
using System.Collections.Generic;

namespace ServiceStack.Common.Extensions
{
	public static class TextExtensions
	{
		public const char ItemSeperator = ',';
		public const char KeyValueSeperator = ':';

		public const char TypeStartChar = '{';
		public const char TypeEndChar = '}';

		public const char MapStartChar = '{';
		public const char MapEndChar = '}';

		public const char ListStartChar = '[';
		public const char ListEndChar = ']';

		//public const string MapStartChar = "";
		//public const string MapEndChar = "";
		//public const string ListStartChar = "";
		//public const string ListEndChar = "";

		public const char PropertyNameSeperator = ':';
		public const char PropertyItemSeperator = '\t';
		public const char QuoteChar = '"';
		public const string QuoteString = "\"";
		public const string DoubleQuoteString = "\"\"";

		static readonly char[] CsvChars = new[] { ItemSeperator, QuoteChar };
		static readonly char[] EscapeChars = new[] { ItemSeperator, QuoteChar, TypeStartChar, TypeEndChar };

		public static string ToCsvField(this string text)
		{
			return string.IsNullOrEmpty(text) || text.IndexOfAny(EscapeChars) == -1
				? text
				: string.Concat
					(
						QuoteString, 
						text.Replace(QuoteString, DoubleQuoteString), 
						QuoteString
					);
		}

		public static string FromCsvField(this string text)
		{
			return string.IsNullOrEmpty(text) || text[0] != QuoteChar
				? text
				: text.Substring(1, text.Length - 2)
					.Replace(DoubleQuoteString, QuoteString);
		}

		public static List<string> FromCsvFields(this IEnumerable<string> texts)
		{
			var safeTexts = new List<string>();
			foreach (var text in texts)
			{
				safeTexts.Add(FromCsvField(text));
			}
			return safeTexts;
		}

		public static string[] FromCsvFields(params string[] texts)
		{
			var textsLen = texts.Length;
			var safeTexts = new string[textsLen];
			for (var i = 0; i < textsLen; i++)
			{
				safeTexts[i] = FromCsvField(texts[i]);
			}
			return safeTexts;
		}
	}
}