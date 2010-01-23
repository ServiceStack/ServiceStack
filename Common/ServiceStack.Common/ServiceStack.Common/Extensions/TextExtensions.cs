using System;
using System.Collections.Generic;
using ServiceStack.Common.Text;

namespace ServiceStack.Common.Extensions
{
	public static class TextExtensions
	{
		public static string ToCsvField(this string text)
		{
			return string.IsNullOrEmpty(text) || text.IndexOfAny(StringSerializer.EscapeChars) == -1
				? text
				: string.Concat
					(
						StringSerializer.QuoteString,
						text.Replace(StringSerializer.QuoteString, StringSerializer.DoubleQuoteString),
						StringSerializer.QuoteString
					);
		}

		public static string FromCsvField(this string text)
		{
			return string.IsNullOrEmpty(text) || text[0] != StringSerializer.QuoteChar
				? text
				: text.Substring(1, text.Length - 2)
					.Replace(StringSerializer.DoubleQuoteString, StringSerializer.QuoteString);
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