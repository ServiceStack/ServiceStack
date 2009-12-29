using System.Collections.Generic;

namespace ServiceStack.Common.Extensions
{
	public static class TextExtensions
	{
		public const char FieldSeperator = ',';
		public const char KeySeperator = ':';

		const char EscapeQuote = '"';
		const char EncodeChar = '%';
		static readonly char[] IllegalChars = new[] { FieldSeperator, KeySeperator };
		static readonly char[] EscapeChars = new[] { FieldSeperator, KeySeperator, EscapeQuote, EncodeChar };

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