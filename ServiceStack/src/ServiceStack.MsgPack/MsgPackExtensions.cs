// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System;
using System.IO;
using ServiceStack.Text;

namespace ServiceStack.MsgPack
{
    public static class MsgPackExtensions
    {
        public static byte[] ToMsgPack<T>(this T obj)
        {
            if (obj == null) return TypeConstants.EmptyByteArray;
            using var ms = MemoryStreamFactory.GetStream();
            MsgPackFormat.Serialize(obj, ms);
            return ms.ToArray();
        }

        public static T FromMsgPack<T>(this byte[] bytes)
        {
            if (bytes == null) return default;
            using var ms = MemoryStreamFactory.GetStream(bytes);
            return MsgPackFormat.Deserialize<T>(ms);
        }
    }
}