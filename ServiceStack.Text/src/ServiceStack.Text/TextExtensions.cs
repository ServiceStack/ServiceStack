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

namespace ServiceStack;

public static class TextExtensions
{
    internal static bool StartsWithFormula(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        var first = value[0];
        if (first == '=' || first == '@' || first == '\t' || first == '\r')
            return true;
        if (first == '+' || first == '-')
        {
            if (value.Length > 1 && (char.IsDigit(value[1]) || value[1] == '.'))
                return false;
            return true;
        }
        return false;
    }

    public static string ToCsvField(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        if (CsvConfig.EscapeFormulas && StartsWithFormula(text))
            text = "'" + text;

        if (!CsvWriter.HasAnyEscapeChars(text))
            return text;

        var itemDelim = CsvConfig.ItemDelimiterString;
        return string.Concat(
            itemDelim,
            text.Replace(itemDelim, CsvConfig.EscapedItemDelimiterString),
            itemDelim);
    }

    public static object ToCsvField(this object text)
    {
        var textSerialized = text is string 
            ? text.ToString() 
            : TypeSerializer.SerializeToString(text).StripQuotes();

        if (textSerialized.IsNullOrEmpty())
            return textSerialized;

        if (CsvConfig.EscapeFormulas && StartsWithFormula(textSerialized))
            textSerialized = "'" + textSerialized;

        if (!CsvWriter.HasAnyEscapeChars(textSerialized))
            return textSerialized;
            
        var itemDelim = CsvConfig.ItemDelimiterString;
        return string.Concat(
            itemDelim,
            textSerialized.Replace(itemDelim, CsvConfig.EscapedItemDelimiterString),
            itemDelim);
    }

    public static string FromCsvField(this string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var itemDelim = CsvConfig.ItemDelimiterString;
        if (text.StartsWith(itemDelim, StringComparison.Ordinal))
        {
            var escapedDelim = CsvConfig.EscapedItemDelimiterString;
            text = text.Substring(itemDelim.Length, text.Length - escapedDelim.Length)
                .Replace(escapedDelim, itemDelim);
        }

        if (CsvConfig.EscapeFormulas && text.Length > 1 && text[0] == '\'' && StartsWithFormula(text.Substring(1)))
        {
            text = text.Substring(1);
        }

        return text;
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