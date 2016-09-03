#if NETSTANDARD1_3
using System.Net.Sockets;

namespace ServiceStack
{
    public static class NetCoreExtensions
    {
        public static void Close(this Socket socket)
        {
            socket.Dispose();
        }
    }
}
#endif