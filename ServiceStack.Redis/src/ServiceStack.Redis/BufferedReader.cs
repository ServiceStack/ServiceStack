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
        private readonly Stream _source;
        readonly byte[] _buffer;
        private int _offset, _available;
        public void Dispose()
        {
            _available = 0;
            _source.Dispose();
        }
        internal void Close()
        {
            _available = 0;
            _source.Close();
        }

        internal BufferedReader(Stream source, int bufferSize)
        {
            _source = source;
            _buffer = new byte[bufferSize];
            Reset();
        }

        internal void Reset()
        {
            _offset = _available = 0;
        }

        internal int ReadByte()
            => _available > 0 ? ReadByteFromBuffer() : ReadByteSlow();

        private int ReadByteFromBuffer()
        {
            --_available;
            return _buffer[_offset++];
        }

        private int ReadByteSlow()
        {
            _available = _source.Read(_buffer, _offset = 0, _buffer.Length);
            return _available > 0 ? ReadByteFromBuffer() : -1;
        }


        private int ReadFromBuffer(byte[] buffer, int offset, int count)
        {
            // we have data in the buffer; hand it back
            if (_available < count) count = _available;
            Buffer.BlockCopy(_buffer, _offset, buffer, offset, count);
            _available -= count;
            _offset += count;
            return count;
        }

        internal int Read(byte[] buffer, int offset, int count)
            => _available > 0
            ? ReadFromBuffer(buffer, offset, count)
            : ReadSlow(buffer, offset, count);

        private int ReadSlow(byte[] buffer, int offset, int count)
        {
            // if they're asking for more than we deal in, just step out of the way
            if (count >= buffer.Length)
                return _source.Read(buffer, offset, count);

            // they're asking for less, so we could still have some left
            _available = _source.Read(_buffer, _offset = 0, _buffer.Length);
            return _available > 0 ? ReadFromBuffer(buffer, offset, count) : 0;
        }
    }
}
