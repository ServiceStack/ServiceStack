using System;
using System.IO;

namespace ServiceStack.Redis
{
    /// <summary>
    /// BufferedReader is a minimal buffer implementation that provides
    /// efficient sync and async access for byte-by-byte consumption;
    /// like BufferedStream, but with the async part
    /// </summary>
    internal sealed partial class BufferedReader : IDisposable
    {
        private readonly Stream source;
        readonly byte[] buffer;
        private int offset, available;
        public void Dispose()
        {
            available = 0;
            source.Dispose();
        }
        internal void Close()
        {
            available = 0;
            source.Close();
        }

        internal BufferedReader(Stream source, int bufferSize)
        {
            this.source = source;
            buffer = new byte[bufferSize];
            Reset();
        }

        internal void Reset()
        {
            offset = available = 0;
        }

        internal int ReadByte()
            => available > 0 ? ReadByteFromBuffer() : ReadByteSlow();

        private int ReadByteFromBuffer()
        {
            --available;
            return buffer[offset++];
        }

        private int ReadByteSlow()
        {
            available = source.Read(buffer, offset = 0, buffer.Length);
            return available > 0 ? ReadByteFromBuffer() : -1;
        }


        private int ReadFromBuffer(byte[] buffer, int offset, int count)
        {
            // we have data in the buffer; hand it back
            if (available < count) count = available;
            Buffer.BlockCopy(this.buffer, this.offset, buffer, offset, count);
            available -= count;
            this.offset += count;
            return count;
        }

        internal int Read(byte[] buffer, int offset, int count)
            => available > 0
            ? ReadFromBuffer(buffer, offset, count)
            : ReadSlow(buffer, offset, count);

        private int ReadSlow(byte[] buffer, int offset, int count)
        {
            // if they're asking for more than we deal in, just step out of the way
            if (count >= buffer.Length)
                return source.Read(buffer, offset, count);

            // they're asking for less, so we could still have some left
            available = source.Read(this.buffer, this.offset = 0, this.buffer.Length);
            return available > 0 ? ReadFromBuffer(buffer, offset, count) : 0;
        }
    }
}
