using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceStack.Messaging.Rcon
{
    internal class Packet
    {
        /// <summary>
        /// True if the packet originated on the server.
        /// </summary>
        public bool FromServer { get; internal set; }

        /// <summary>
        /// True if the packet is a response from a sent packet.
        /// </summary>
        public bool IsResponse { get; internal set; }

        /// <summary>
        /// Sequence identifier. Unique to the connection.
        /// </summary>
        public uint Sequence { get; internal set; }

        /// <summary>
        /// Words.
        /// </summary>
        public byte[][] Words { get; internal set; }
    }
}
