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

using System.Collections.Generic;
using System.Text;
using ServiceStack.Text;
using ServiceStack.Text.Common;

namespace ServiceStack
{
    public static class MapExtensions
    {
        public static string Join<K, V>(this Dictionary<K, V> values)
        {
            return Join(values, JsWriter.ItemSeperatorString, JsWriter.MapKeySeperatorString);
        }

        public static string Join<K, V>(this Dictionary<K, V> values, string itemSeperator, string keySeperator)
        {
            var sb = StringBuilderThreadStatic.Allocate();
            foreach (var entry in values)
            {
                if (sb.Length > 0)
                    sb.Append(itemSeperator);

                sb.Append(entry.Key).Append(keySeperator).Append(entry.Value);
            }
            return StringBuilderThreadStatic.ReturnAndFree(sb);
        }
    }
}