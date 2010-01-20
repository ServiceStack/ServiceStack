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
		public const char PropertyNameSeperator = '=';
		public const char PropertyItemSeperator = '\t';
		public const char QuoteChar = '"';
		public const string QuoteString = "\"";
		public const string DoubleQuoteString = "\"\"";

		const char EscapeQuote = '"';
		const char EncodeChar = '%';
		static readonly char[] IllegalChars = new[] { ItemSeperator, KeyValueSeperator, PropertyItemSeperator };
		static readonly char[] EscapeChars = new[] { ItemSeperator, KeyValueSeperator, PropertyItemSeperator, EscapeQuote, EncodeChar };
		static readonly char[] CsvChars = new[] { ItemSeperator, QuoteChar };

		//public static string ToSafeString(this string text)
		//{
		//    return ToCsvField(text);

		//    if (string.IsNullOrEmpty(text)
		//        || (text.IndexOfAny(IllegalChars) == -1 && text[0] != EscapeQuote)) return text;

		//    return EscapeQuote + text.HexEscape(EscapeChars) + EscapeQuote;
		//}

		//public static string FromCsvField(this string text)
		//{
		//    return FromCsvField(text);

		//    if (string.IsNullOrEmpty(text) || (text[0] != EscapeQuote)) return text;

		//    var withoutQuotes = text.Substring(1, text.Length - 2);
		//    return withoutQuotes.HexUnescape(EscapeChars);
		//}

		public static string ToCsvField(this string text)
		{
			return string.IsNullOrEmpty(text) || text.IndexOfAny(CsvChars) == -1
				? text
				: string.Concat(QuoteString, text.Replace(QuoteString, DoubleQuoteString), QuoteString);
		}

		public static string FromCsvField(this string text)
		{
			return string.IsNullOrEmpty(text) || text[0] != QuoteChar
				? text
				: text.Substring(1, text.Length - 2).Replace(DoubleQuoteString, QuoteString);
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