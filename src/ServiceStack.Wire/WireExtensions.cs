// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

namespace ServiceStack.Wire
{
    using global::Wire;
    using ServiceStack.Text;

    public static class WireExtensions
    {
        private static readonly Serializer serializer = new Serializer();

        public static byte[] ToWire<T>(this T obj)
        {
            using (var ms = MemoryStreamFactory.GetStream())
            {
                serializer.Serialize(obj, ms);
                var bytes = ms.ToArray();
                return bytes;
            }
        }

        public static T FromWire<T>(this byte[] bytes)
        {
            using (var ms = MemoryStreamFactory.GetStream(bytes))
            {
                return serializer.Deserialize<T>(ms);
            }
        }
    }
}