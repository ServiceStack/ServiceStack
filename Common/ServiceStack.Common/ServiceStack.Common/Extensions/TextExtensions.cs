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

		const char EscapeQuote = '"';
		const char EncodeChar = '%';
		static readonly char[] IllegalChars = new[] { ItemSeperator, KeyValueSeperator, PropertyItemSeperator };
		static readonly char[] EscapeChars = new[] { ItemSeperator, KeyValueSeperator, PropertyItemSeperator, EscapeQuote, EncodeChar };

		public static string ToSafeString(this string text)
		{
			if (string.IsNullOrEmpty(text)
			    || (text.IndexOfAny(IllegalChars) == -1 && text[0] != EscapeQuote)) return text;

			return EscapeQuote + text.HexEscape(EscapeChars) + EscapeQuote;
		}

		public static string FromSafeString(this string text)
		{
			if (string.IsNullOrEmpty(text) || (text[0] != EscapeQuote)) return text;

			var withoutQuotes = text.Substring(1, text.Length - 2);
			return withoutQuotes.HexUnescape(EscapeChars);
		}

		public static List<string> FromSafeStrings(this IEnumerable<string> texts)
		{
			var safeTexts = new List<string>();
			foreach (var text in texts)
			{
				safeTexts.Add(FromSafeString(text));
			}
			return safeTexts;
		}

		public static string[] FromSafeStrings(params string[] texts)
		{
			var textsLen = texts.Length;
			var safeTexts = new string[textsLen];
			for (var i = 0; i < textsLen; i++)
			{
				safeTexts[i] = FromSafeString(texts[i]);
			}
			return safeTexts;
		}
	}
}