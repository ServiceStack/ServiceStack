// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using ServiceStack.Text;

namespace ServiceStack.ProtoBuf
{
    public static class ProtoBufExtensions
    {
        public static byte[] ToProtoBuf<T>(this T obj)
        {
            if (obj == null) return TypeConstants.EmptyByteArray;
            using var ms = MemoryStreamFactory.GetStream();
            ProtoBufFormat.Serialize(obj, ms);
            return ms.ToArray();
        }

        public static T FromProtoBuf<T>(this byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return default;
            using var ms = MemoryStreamFactory.GetStream(bytes);
            return (T) ProtoBufFormat.Deserialize(typeof(T), ms);
        }
    }
}