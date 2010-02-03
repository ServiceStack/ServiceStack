using System;
using System.Collections.Generic;

namespace ServiceStack.Text
{
	public static class TextExtensions
	{
		public static string ToCsvField(this string text)
		{
			return string.IsNullOrEmpty(text) || text.IndexOfAny(TypeSerializer.EscapeChars) == -1
		       	? text
		       	: string.Concat
		       	  	(
		       	  		TypeSerializer.QuoteString,
		       	  		text.Replace(TypeSerializer.QuoteString, TypeSerializer.DoubleQuoteString),
		       	  		TypeSerializer.QuoteString
		       	  	);
		}

		public static string FromCsvField(this string text)
		{
			const int startingQuotePos = 1;
			const int endingQuotePos = 2;
			return string.IsNullOrEmpty(text) || text[0] != TypeSerializer.QuoteChar
			       	? text
					: text.Substring(startingQuotePos, text.Length - endingQuotePos)
			       	  	.Replace(TypeSerializer.DoubleQuoteString, TypeSerializer.QuoteString);
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