//
// https://github.com/ServiceStack/ServiceStack.Text
// ServiceStack.Text: .NET C# POCO JSON, JSV and CSV Text Serializers.
//
// Authors:
//	 Peter Townsend (townsend.pete@gmail.com)
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2012 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Text.Common;

namespace ServiceStack.Text
{
    public static class JsvFormatter
    {
        public static string Format(string serializedText)
        {
            if (string.IsNullOrEmpty(serializedText)) return null;

            var tabCount = 0;
            var sb = StringBuilderThreadStatic.Allocate();
            var firstKeySeparator = true;
            var inString = false;

            for (var i = 0; i < serializedText.Length; i++)
            {
                var current = serializedText[i];
                var previous = i - 1 >= 0 ? serializedText[i - 1] : 0;
                var next = i < serializedText.Length - 1 ? serializedText[i + 1] : 0;

                if (current == JsWriter.MapStartChar || current == JsWriter.ListStartChar)
                {
                    if (previous == JsWriter.MapKeySeperator)
                    {
                        if (next == JsWriter.MapEndChar || next == JsWriter.ListEndChar)
                        {
                            sb.Append(current);
                            sb.Append(serializedText[++i]); //eat next
                            continue;
                        }

                        AppendTabLine(sb, tabCount);
                    }

                    sb.Append(current);
                    AppendTabLine(sb, ++tabCount);
                    firstKeySeparator = true;
                    continue;
                }

                if (current == JsWriter.MapEndChar || current == JsWriter.ListEndChar)
                {
                    AppendTabLine(sb, --tabCount);
                    sb.Append(current);
                    firstKeySeparator = true;
                    continue;
                }

                if (current == JsWriter.QuoteChar)
                {
                    sb.Append(current);
                    inString = !inString;
                    continue;
                }

                if (current == JsWriter.ItemSeperator && !inString)
                {
                    sb.Append(current);
                    AppendTabLine(sb, tabCount);
                    firstKeySeparator = true;
                    continue;
                }

                sb.Append(current);

                if (current == JsWriter.MapKeySeperator && firstKeySeparator)
                {
                    sb.Append(" ");
                    firstKeySeparator = false;
                }
            }

            return StringBuilderThreadStatic.ReturnAndFree(sb);
        }

        private static void AppendTabLine(StringBuilder sb, int tabCount)
        {
            sb.AppendLine();

            if (tabCount > 0)
            {
                sb.Append(new string('\t', tabCount));
            }
        }
    }
}