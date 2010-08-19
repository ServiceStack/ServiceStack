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
using System.Text;
using ServiceStack.Text.Common;

namespace ServiceStack.Text
{
	public static class TextExtensions
	{
		public static string ToCsvField(this string text)
		{
			return string.IsNullOrEmpty(text) || !JsWriter.HasAnyEscapeChars(text)
		       	? text
		       	: string.Concat
		       	  	(
						JsWriter.QuoteString,
						text.Replace(JsWriter.QuoteString, TypeSerializer.DoubleQuoteString),
						JsWriter.QuoteString
		       	  	);
		}

		public static string FromCsvField(this string text)
		{
			const int startingQuotePos = 1;
			const int endingQuotePos = 2;
			return string.IsNullOrEmpty(text) || text[0] != JsWriter.QuoteChar
			       	? text
					: text.Substring(startingQuotePos, text.Length - endingQuotePos)
						.Replace(TypeSerializer.DoubleQuoteString, JsWriter.QuoteString);
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

		public static string SerializeToString<T>(this T value)
		{
			return TypeSerializer.SerializeToString(value);
		}

		public static byte[] SerializeToUtf8Bytes<T>(this T value)
		{
			return Encoding.UTF8.GetBytes(TypeSerializer.SerializeToString(value));
		}

		public static T DeserializeFromString<T>(this string serializedObj)
		{
			return TypeSerializer.DeserializeFromString<T>(serializedObj);
		}

		public static T DeserializeFromUtf8Bytes<T>(this byte[] serializedbytes)
		{
			var serializedObj = Encoding.UTF8.GetString(serializedbytes, 0, serializedbytes.Length);
			return TypeSerializer.DeserializeFromString<T>(serializedObj);
		}
	}
}