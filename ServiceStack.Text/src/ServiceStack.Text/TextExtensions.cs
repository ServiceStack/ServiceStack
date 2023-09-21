//
// https://github.com/ServiceStack/ServiceStack.Text
// ServiceStack.Text: .NET C# POCO JSON, JSV and CSV Text Serializers.
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2012 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Collections.Generic;
using ServiceStack.Text;

namespace ServiceStack
{
    public static class TextExtensions
    {
        public static string ToCsvField(this string text)
        {
            var itemDelim = CsvConfig.ItemDelimiterString;
            return string.IsNullOrEmpty(text) || !CsvWriter.HasAnyEscapeChars(text)
                ? text
                : string.Concat(
                    itemDelim,
                    text.Replace(itemDelim, CsvConfig.EscapedItemDelimiterString),
                    itemDelim);
        }

        public static object ToCsvField(this object text)
        {
            var textSerialized = text is string 
                ? text.ToString() 
                : TypeSerializer.SerializeToString(text).StripQuotes();

            if (textSerialized.IsNullOrEmpty() || !CsvWriter.HasAnyEscapeChars(textSerialized))
                return textSerialized;
            
            var itemDelim = CsvConfig.ItemDelimiterString;
            return string.Concat(
                itemDelim,
                textSerialized.Replace(itemDelim, CsvConfig.EscapedItemDelimiterString),
                itemDelim);
        }

        public static string FromCsvField(this string text)
        {
            var itemDelim = CsvConfig.ItemDelimiterString;
            if (string.IsNullOrEmpty(text) || !text.StartsWith(itemDelim, StringComparison.Ordinal))
                return text; 
            var escapedDelim = CsvConfig.EscapedItemDelimiterString;
            return text.Substring(itemDelim.Length, text.Length - escapedDelim.Length)
                .Replace(escapedDelim, itemDelim);
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
            return JsonSerializer.SerializeToString(value);
        }
    }
}