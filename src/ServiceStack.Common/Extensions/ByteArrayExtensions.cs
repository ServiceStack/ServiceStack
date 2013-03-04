using System;
using Proxy = ServiceStack.Common.ByteArrayExtensions;

namespace ServiceStack.Common.Extensions
{
    [Obsolete("Use ServiceStack.Common.ByteArrayExtensions")]
    public static class ByteArrayExtensions
    {
        public static bool AreEqual(this byte[] b1, byte[] b2)
        {
            return Proxy.AreEqual(b1, b2);
        }
    }
}