using ServiceStack.Redis.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis
{
    internal sealed partial class BufferedReader
    {
        internal ValueTask<int> ReadByteAsync(in CancellationToken token = default)
        => _available > 0 ? ReadByteFromBuffer().AsValueTaskResult() : ReadByteSlowAsync(token);

        private ValueTask<int> ReadByteSlowAsync(in CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            _offset = 0;
#if ASYNC_MEMORY
            var pending = _source.ReadAsync(new Memory<byte>(_buffer), token);
            if (!pending.IsCompletedSuccessfully)
                return Awaited(this, pending);
#else
            var pending = _source.ReadAsync(_buffer, 0, _buffer.Length, token);
            if (pending.Status != TaskStatus.RanToCompletion)
                return Awaited(this, pending);
#endif

            _available = pending.Result;
            return (_available > 0 ? ReadByteFromBuffer() : -1).AsValueTaskResult();

#if ASYNC_MEMORY
            static async ValueTask<int> Awaited(BufferedReader @this, ValueTask<int> pending)
            {
                @this._available = await pending.ConfigureAwait(false);
                return @this._available > 0 ? @this.ReadByteFromBuffer() : -1;
            }
#else
            static async ValueTask<int> Awaited(BufferedReader @this, Task<int> pending)
            {
                @this._available = await pending.ConfigureAwait(false);
                return @this._available > 0 ? @this.ReadByteFromBuffer() : -1;
            }
#endif
        }

        internal ValueTask<int> ReadAsync(byte[] buffer, int offset, int count, in CancellationToken token = default)
            => _available > 0
            ? ReadFromBuffer(buffer, offset, count).AsValueTaskResult()
            : ReadSlowAsync(buffer, offset, count, token);

        private ValueTask<int> ReadSlowAsync(byte[] buffer, int offset, int count, in CancellationToken token)
        {
            // if they're asking for more than we deal in, just step out of the way
            if (count >= buffer.Length)
            {
#if ASYNC_MEMORY
                return _source.ReadAsync(new Memory<byte>(buffer, offset, count), token);
#else
                return new ValueTask<int>(_source.ReadAsync(buffer, offset, count, token));
#endif
            }

            // they're asking for less, so we could still have some left
            _offset = 0;
#if ASYNC_MEMORY
            var pending = _source.ReadAsync(new Memory<byte>(_buffer), token);
            if (!pending.IsCompletedSuccessfully)
                return Awaited(this, pending, buffer, offset, count);

            _available = pending.Result; // already checked status, this is fine
            return (_available > 0 ? ReadFromBuffer(buffer, offset, count) : 0).AsValueTaskResult();

            static async ValueTask<int> Awaited(BufferedReader @this, ValueTask<int> pending, byte[] buffer, int offset, int count)
            {
                @this._available = await pending.ConfigureAwait(false);
                return @this._available > 0 ? @this.ReadFromBuffer(buffer, offset, count) : 0;
            }
#else
            var pending = _source.ReadAsync(_buffer, 0, _buffer.Length, token);
            if (pending.Status != TaskStatus.RanToCompletion)
                return Awaited(this, pending, buffer, offset, count);

            _available = pending.Result; // already checked status, this is fine
            return (_available > 0 ? ReadFromBuffer(buffer, offset, count) : 0).AsValueTaskResult();
            
            static async ValueTask<int> Awaited(BufferedReader @this, Task<int> pending, byte[] buffer, int offset, int count)
            {
                @this._available = await pending.ConfigureAwait(false);
                return @this._available > 0 ? @this.ReadFromBuffer(buffer, offset, count) : 0;
            }
#endif
        }
    }
}
