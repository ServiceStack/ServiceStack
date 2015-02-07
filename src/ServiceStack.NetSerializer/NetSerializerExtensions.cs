// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System.IO;
using ServiceStack.Text;

namespace ServiceStack.NetSerializer
{
    public static class NetSerializerExtensions
    {
        public static byte[] ToNetSerializer<T>(this T obj)
        {
            using (var ms = MemoryStreamFactory.GetStream())
            {
                NetSerializerFormat.Serialize(obj, ms);
                var bytes = ms.ToArray();
                return bytes;
            }
        }

        public static T FromNetSerializer<T>(this byte[] bytes)
        {
            using (var ms = MemoryStreamFactory.GetStream(bytes))
            {
                var obj = (T)NetSerializerFormat.Deserialize(typeof(T), ms);
                return obj;
            }
        }
    }
}