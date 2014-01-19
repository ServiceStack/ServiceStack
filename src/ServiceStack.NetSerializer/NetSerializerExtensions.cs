// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.IO;

namespace ServiceStack.NetSerializer
{
    public static class NetSerializerExtensions
    {
        public static byte[] ToNetSerializer<T>(this T obj)
        {
            using (var ms = new MemoryStream())
            {
                NetSerializerFormat.Serialize(obj, ms);
                var bytes = ms.ToArray();
                return bytes;
            }
        }

        public static T FromNetSerializer<T>(this byte[] bytes)
        {
            using (var ms = new MemoryStream(bytes))
            {
                var obj = (T)NetSerializerFormat.Deserialize(typeof(T), ms);
                return obj;
            }
        }
    }
}