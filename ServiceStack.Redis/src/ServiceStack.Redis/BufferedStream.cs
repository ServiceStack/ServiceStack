#if NETCORE
using System;
using System.IO;
using System.Net.Sockets;

namespace ServiceStack.Redis
{
    // recommendation: mark this obsolete as it is incomplete, and no longer used;
    // I've marked it obsolete in DEBUG to be sure
#if DEBUG
    [Obsolete("Prefer System.IO.BufferedStream")]
#endif
    public sealed class BufferedStream : Stream
    {
        Stream networkStream;

        public BufferedStream(Stream stream)
            : this(stream, 0) {}

        public BufferedStream(Stream stream, int bufferSize)
        {
            networkStream = stream;
        }
        public override bool CanRead => networkStream.CanRead;

        public override bool CanSeek => networkStream.CanSeek;

        public override bool CanWrite => networkStream.CanWrite;

        public override long Position
        {
            get { return networkStream.Position; }
            set { networkStream.Position = value; }
        }

        public override long Length => networkStream.Length;

        public override int Read(byte[] buffer, int offset, int length) => networkStream.Read(buffer, offset, length);

        public override void Write(byte[] buffer, int offset, int length) => networkStream.Write(buffer, offset, length);

        public override void Flush() => networkStream.Flush();

        public override void SetLength(long length) => networkStream.SetLength(length);

        public override long Seek(long position, SeekOrigin origin) => networkStream.Seek(position, origin);
    }
}
#endif