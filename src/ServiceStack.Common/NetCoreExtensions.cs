#if NETSTANDARD2_0
using System;
using System.Data.Common;
using System.Data;
using System.Net.Sockets;

namespace ServiceStack
{
    public static class NetCoreExtensions
    {
        public static void Close(this Socket socket)
        {
            socket.Dispose();
        }

        public static void Close(this DbDataReader reader)
        {
            reader.Dispose();
        }
}
}
#endif