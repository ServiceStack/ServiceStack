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
            return string.IsNullOrEmpty(text) || !CsvWriter.HasAnyEscapeChars(text)
                ? text
                : string.Concat
                        (
                            CsvConfig.ItemDelimiterString,
                            text.Replace(CsvConfig.ItemDelimiterString, CsvConfig.EscapedItemDelimiterString),
                            CsvConfig.ItemDelimiterString
                        );
        }

        public static object ToCsvField(this object text)
        {
            var textSerialized = string.Empty;
            if (text is string)
            {
                textSerialized = text.ToString();
            }
            else
            {
                textSerialized = TypeSerializer.SerializeToString(text).StripQuotes();
            }

            return textSerialized.IsNullOrEmpty() || !CsvWriter.HasAnyEscapeChars(textSerialized)
                ? textSerialized
                : string.Concat
                  (
                      CsvConfig.ItemDelimiterString,
                      textSerialized.Replace(CsvConfig.ItemDelimiterString, CsvConfig.EscapedItemDelimiterString),
                      CsvConfig.ItemDelimiterString
                  );
        }

        public static string FromCsvField(this string text)
        {
            return string.IsNullOrEmpty(text) || !text.StartsWith(CsvConfig.ItemDelimiterString, StringComparison.Ordinal)
                ? text
                : text.Substring(CsvConfig.ItemDelimiterString.Length, text.Length - CsvConfig.EscapedItemDelimiterString.Length)
                    .Replace(CsvConfig.EscapedItemDelimiterString, CsvConfig.ItemDelimiterString);
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