// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.IO;

namespace ServiceStack.ProtoBuf
{
    public static class ProtoBufExtensions
    {
        public static byte[] ToProtoBuf<T>(this T obj)
        {
            using (var ms = new MemoryStream())
            {
                ProtoBufFormat.Serialize(obj, ms);
                var bytes = ms.ToArray();
                return bytes;
            }
        }

        public static T FromProtoBuf<T>(this byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                var obj = (T)ProtoBufFormat.Deserialize(typeof(T), ms);
                return obj;
            }
        }
    }
}