#if NETSTANDARD1_3
using System.Net.Sockets;

namespace ServiceStack
{
    /// <summary>
    /// Useful IPAddressExtensions from: 
    /// http://blogs.msdn.com/knom/archive/2008/12/31/ip-address-calculations-with-c-subnetmasks-networks.aspx
    /// 
    /// </summary>
    public static class SocketExtensions
    {
        public static void Close(this Socket socket)
        {
            socket.Dispose();
        }
    }
}
#endif