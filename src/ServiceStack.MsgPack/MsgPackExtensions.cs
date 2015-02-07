// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


using System.IO;
using ServiceStack.Text;

namespace ServiceStack.MsgPack
{
    public static class MsgPackExtensions
    {
        public static byte[] ToMsgPack<T>(this T obj)
        {
            using (var ms = MemoryStreamFactory.GetStream())
            {
                MsgPackFormat.Serialize(obj, ms);
                var bytes = ms.ToArray();
                return bytes;
            }
        }

        public static T FromMsgPack<T>(this byte[] bytes)
        {
            using (var ms = MemoryStreamFactory.GetStream(bytes))
            {
                var obj = (T)MsgPackFormat.Deserialize(typeof(T), ms);
                return obj;
            }
        }
    }
}