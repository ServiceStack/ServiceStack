using ServiceStack.Redis.Internal;
using ServiceStack.Redis.Pipeline;
using ServiceStack.Text;
using ServiceStack.Text.Pools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis
{
    partial class RedisNativeClient
    {
        private async ValueTask<byte[][]> SendExpectMultiDataAsync(CancellationToken token, params byte[][] cmdWithBinaryArgs)
        {
            return (await SendReceiveAsync(cmdWithBinaryArgs, ReadMultiDataAsync, token,
                PipelineAsync != null ? PipelineAsync.CompleteMultiBytesQueuedCommandAsync : (Action<Func<CancellationToken, ValueTask<byte[][]>>>)null).ConfigureAwait(false))
            ?? TypeConstants.EmptyByteArrayArray;
        }

        protected ValueTask SendWithoutReadAsync(CancellationToken token, params byte[][] cmdWithBinaryArgs)
            => SendReceiveAsync<long>(cmdWithBinaryArgs, null, token, null, sendWithoutRead: true).Await();

        private ValueTask<long> SendExpectLongAsync(CancellationToken token, params byte[][] cmdWithBinaryArgs)
        {
            return SendReceiveAsync(cmdWithBinaryArgs, ReadLongAsync, token,
                PipelineAsync != null ? PipelineAsync.CompleteLongQueuedCommandAsync : (Action<Func<CancellationToken, ValueTask<long>>>)null);
        }

        private ValueTask<double> SendExpectDoubleAsync(CancellationToken token, params byte[][] cmdWithBinaryArgs)
        {
            return SendReceiveAsync(cmdWithBinaryArgs, ReadDoubleAsync, token,
                PipelineAsync != null ? PipelineAsync.CompleteDoubleQueuedCommandAsync : (Action<Func<CancellationToken, ValueTask<double>>>)null);
        }
        protected ValueTask<string> SendExpectStringAsync(CancellationToken token, params byte[][] cmdWithBinaryArgs)
            => SendExpectDataAsync(token, cmdWithBinaryArgs).FromUtf8BytesAsync();

        private ValueTask SendExpectSuccessAsync(CancellationToken token, params byte[][] cmdWithBinaryArgs)
        {
            //Turn Action into Func Hack
            Action<Func<CancellationToken, ValueTask<long>>> completePipelineFn = null;
            if (PipelineAsync != null) completePipelineFn = f => { PipelineAsync.CompleteVoidQueuedCommandAsync(ct => f(ct).Await()); };

            return SendReceiveAsync(cmdWithBinaryArgs, ExpectSuccessFnAsync, token, completePipelineFn).Await();
        }

        private ValueTask<byte[]> SendExpectDataAsync(CancellationToken token, params byte[][] cmdWithBinaryArgs)
        {
            return SendReceiveAsync(cmdWithBinaryArgs, ReadDataAsync, token, PipelineAsync != null ? PipelineAsync.CompleteBytesQueuedCommandAsync : (Action<Func<CancellationToken, ValueTask<byte[]>>>)null);
        }

        private ValueTask<string> SendExpectCodeAsync(CancellationToken token, params byte[][] cmdWithBinaryArgs)
        {
            return SendReceiveAsync(cmdWithBinaryArgs, ExpectCodeAsync, token, PipelineAsync != null ? PipelineAsync.CompleteStringQueuedCommandAsync : (Action<Func<CancellationToken, ValueTask<string>>>)null);
        }

        private ValueTask<ScanResult> SendExpectScanResultAsync(CancellationToken token, byte[] cmd, params byte[][] args)
        {
            var cmdWithArgs = MergeCommandWithArgs(cmd, args);
            return SendExpectDeeplyNestedMultiDataAsync(token, cmdWithArgs).Await(multiData => ParseScanResult(multiData));
        }

        private ValueTask<object[]> SendExpectDeeplyNestedMultiDataAsync(CancellationToken token, params byte[][] cmdWithBinaryArgs)
            => SendReceiveAsync(cmdWithBinaryArgs, ReadDeeplyNestedMultiDataAsync, token);

        private ValueTask<object[]> ReadDeeplyNestedMultiDataAsync(CancellationToken token)
            => ReadDeeplyNestedMultiDataItemAsync(token).Await(result => (object[])result);

        private async ValueTask<object> ReadDeeplyNestedMultiDataItemAsync(CancellationToken token)
        {
            int c = await SafeReadByteAsync(token).ConfigureAwait(false);
            if (c == -1)
                throw CreateNoMoreDataError();

            var s = await ReadLineAsync(token).ConfigureAwait(false);
            if (log.IsDebugEnabled)
                Log("R: {0}", s);

            switch (c)
            {
                case '$':
                    return await ParseSingleLineAsync(string.Concat(char.ToString((char)c), s), token).ConfigureAwait(false);

                case '-':
                    throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);

                case '*':
                    if (int.TryParse(s, out var count))
                    {
                        var array = new object[count];
                        for (int i = 0; i < count; i++)
                        {
                            array[i] = await ReadDeeplyNestedMultiDataItemAsync(token).ConfigureAwait(false);
                        }

                        return array;
                    }
                    break;

                default:
                    return s;
            }

            throw CreateResponseError("Unknown reply on multi-request: " + ((char)c) + s); // c here is the protocol prefix
        }

        protected ValueTask<RedisData> SendExpectComplexResponseAsync(CancellationToken token, params byte[][] cmdWithBinaryArgs)
        {
            return SendReceiveAsync(cmdWithBinaryArgs, ReadComplexResponseAsync, token,
                PipelineAsync != null ? PipelineAsync.CompleteRedisDataQueuedCommandAsync : (Action<Func<CancellationToken, ValueTask<RedisData>>>)null);
        }

        private async ValueTask<RedisData> ReadComplexResponseAsync(CancellationToken token)
        {
            int c = await SafeReadByteAsync(token).ConfigureAwait(false);
            if (c == -1)
                throw CreateNoMoreDataError();

            var s = await ReadLineAsync(token).ConfigureAwait(false);
            if (log.IsDebugEnabled)
                Log("R: {0}", s);

            switch (c)
            {
                case '$':
                    return new RedisData
                    {
                        Data = await ParseSingleLineAsync(string.Concat(char.ToString((char)c), s), token).ConfigureAwait(false)
                    };

                case '-':
                    throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);

                case '*':
                    if (int.TryParse(s, out var count))
                    {
                        var ret = new RedisData { Children = new List<RedisData>() };
                        for (var i = 0; i < count; i++)
                        {
                            ret.Children.Add(await ReadComplexResponseAsync(token).ConfigureAwait(false));
                        }

                        return ret;
                    }
                    break;

                default:
                    return new RedisData { Data = s.ToUtf8Bytes() };
            }

            throw CreateResponseError("Unknown reply on multi-request: " + ((char)c) + s); // c here is the protocol prefix
        }

        private async ValueTask<T> SendReceiveAsync<T>(byte[][] cmdWithBinaryArgs,
            Func<CancellationToken, ValueTask<T>> fn,
            CancellationToken token,
            Action<Func<CancellationToken, ValueTask<T>>> completePipelineFn = null,
            bool sendWithoutRead = false)
        {
            //if (TrackThread != null)
            //{
            //    if (TrackThread.Value.ThreadId != Thread.CurrentThread.ManagedThreadId)
            //        throw new InvalidAccessException(TrackThread.Value.ThreadId, TrackThread.Value.StackTrace);
            //}

            var i = 0;
            var didWriteToBuffer = false;
            Exception originalEx = null;

            var firstAttempt = DateTime.UtcNow;

            while (true)
            {
                // this is deliberately *before* the try, so we never retry
                // if we've been cancelled
                token.ThrowIfCancellationRequested();
                try
                {
                    if (TryConnectIfNeeded()) // TODO: asyncify
                        didWriteToBuffer = false;

                    if (socket == null)
                        throw new RedisRetryableException("Socket is not connected");

                    if (!didWriteToBuffer) //only write to buffer once
                    {
                        WriteCommandToSendBuffer(cmdWithBinaryArgs);
                        didWriteToBuffer = true;
                    }

                    if (PipelineAsync == null) //pipeline will handle flush if in pipeline
                    {
                        await FlushSendBufferAsync(token).ConfigureAwait(false);
                    }
                    else if (!sendWithoutRead)
                    {
                        if (completePipelineFn == null)
                            throw new NotSupportedException("Pipeline is not supported.");

                        completePipelineFn(fn);
                        return default;
                    }

                    var result = default(T);
                    if (fn != null)
                        result = await fn(token).ConfigureAwait(false);

                    if (Pipeline == null)
                        ResetSendBuffer();

                    if (i > 0)
                        Interlocked.Increment(ref RedisState.TotalRetrySuccess);

                    Interlocked.Increment(ref RedisState.TotalCommandsSent);

                    return result;
                }
                catch (Exception outerEx)
                {
                    if (log.IsDebugEnabled)
                        logDebug("SendReceive Exception: " + outerEx.Message);

                    var retryableEx = outerEx as RedisRetryableException;
                    if (retryableEx == null && outerEx is RedisException
                        || outerEx is LicenseException)
                    {
                        ResetSendBuffer();
                        throw;
                    }

                    var ex = retryableEx ?? GetRetryableException(outerEx);
                    if (ex == null)
                        throw CreateConnectionError(originalEx ?? outerEx);

                    if (originalEx == null)
                        originalEx = ex;

                    var retry = DateTime.UtcNow - firstAttempt < retryTimeout;
                    if (!retry)
                    {
                        if (Pipeline == null)
                            ResetSendBuffer();

                        Interlocked.Increment(ref RedisState.TotalRetryTimedout);
                        throw CreateRetryTimeoutException(retryTimeout, originalEx);
                    }

                    Interlocked.Increment(ref RedisState.TotalRetryCount);
                    await Task.Delay(GetBackOffMultiplier(++i), token).ConfigureAwait(false);
                }
            }
        }

        internal ValueTask FlushSendBufferAsync(CancellationToken token)
        {
            if (currentBufferIndex > 0)
                PushCurrentBuffer();

            if (cmdBuffer.Count > 0)
            {
                OnBeforeFlush?.Invoke();

                if (!Env.IsMono && sslStream == null)
                {
                    if (log.IsDebugEnabled && RedisConfig.EnableVerboseLogging)
                    {
                        var sb = StringBuilderCache.Allocate();
                        foreach (var cmd in cmdBuffer)
                        {
                            if (sb.Length > 50)
                                break;

                            sb.Append(Encoding.UTF8.GetString(cmd.Array, cmd.Offset, cmd.Count));
                        }
                        logDebug("socket.Send: " + StringBuilderCache.ReturnAndFree(sb.Replace("\r\n", " ")).SafeSubstring(0, 50));
                    }

                    return new ValueTask(socket.SendAsync(cmdBuffer, SocketFlags.None));
                }
                else
                {
                    //Sending IList<ArraySegment> Throws 'Message to Large' SocketException in Mono
                    if (sslStream == null)
                    {
                        foreach (var segment in cmdBuffer)
                        {   // TODO: what is modern Mono behavior here?
                            socket.Send(segment.Array, segment.Offset, segment.Count, SocketFlags.None);
                        }
                    }
                    else
                    {
                        return WriteAsync(sslStream, cmdBuffer, token);
                    }
                }
            }

            return default;

            static async ValueTask WriteAsync(Stream destination, List<ArraySegment<byte>> buffer, CancellationToken token)
            {
                foreach (var segment in buffer)
                {
#if ASYNC_MEMORY
                    await destination.WriteAsync(new ReadOnlyMemory<byte>(segment.Array, segment.Offset, segment.Count), token).ConfigureAwait(false);
#else
                    await destination.WriteAsync(segment.Array, segment.Offset, segment.Count, token).ConfigureAwait(false);
#endif
                }
            }
        }


        private ValueTask<int> SafeReadByteAsync(in CancellationToken token, [CallerMemberName]string name = null)
        {
            AssertNotDisposed();

            if (log.IsDebugEnabled && RedisConfig.EnableVerboseLogging)
                logDebug(name + "()");

            return bufferedReader.ReadByteAsync(token);
        }

        private async ValueTask<string> ReadLineAsync(CancellationToken token)
        {
            AssertNotDisposed();

            var sb = StringBuilderCache.Allocate();

            int c;
            while ((c = await bufferedReader.ReadByteAsync(token).ConfigureAwait(false)) != -1)
            {
                if (c == '\r')
                    continue;
                if (c == '\n')
                    break;
                sb.Append((char)c);
            }
            return StringBuilderCache.ReturnAndFree(sb);
        }

        private async ValueTask<byte[]> ParseSingleLineAsync(string r, CancellationToken token)
        {
            if (log.IsDebugEnabled)
                Log("R: {0}", r);
            if (r.Length == 0)
                throw CreateResponseError("Zero length response");

            char c = r[0];
            if (c == '-')
                throw CreateResponseError(r.StartsWith("-ERR") ? r.Substring(5) : r.Substring(1));

            if (c == '$')
            {
                if (r == "$-1")
                    return null;

                if (int.TryParse(r.Substring(1), out var count))
                {
                    var retbuf = new byte[count];

                    var offset = 0;
                    while (count > 0)
                    {
                        var readCount = await bufferedReader.ReadAsync(retbuf, offset, count, token).ConfigureAwait(false);
                        if (readCount <= 0)
                            throw CreateResponseError("Unexpected end of Stream");

                        offset += readCount;
                        count -= readCount;
                    }

                    if (await bufferedReader.ReadByteAsync(token).ConfigureAwait(false) != '\r'
                        || await bufferedReader.ReadByteAsync(token).ConfigureAwait(false) != '\n')
                        throw CreateResponseError("Invalid termination");

                    return retbuf;
                }
                throw CreateResponseError("Invalid length");
            }

            if (c == ':' || c == '+')
            {
                //match the return value
                return r.Substring(1).ToUtf8Bytes();
            }
            throw CreateResponseError("Unexpected reply: " + r);
        }

        private ValueTask<byte[]> ReadDataAsync(CancellationToken token)
        {
            var pending = ReadLineAsync(token);
            return pending.IsCompletedSuccessfully
                ? ParseSingleLineAsync(pending.Result, token)
                : Awaited(this, pending, token);

            static async ValueTask<byte[]> Awaited(RedisNativeClient @this, ValueTask<string> pending, CancellationToken token)
            {
                var r = await pending.ConfigureAwait(false);
                return await @this.ParseSingleLineAsync(r, token).ConfigureAwait(false);
            }
        }

        private async ValueTask<string> ExpectCodeAsync(CancellationToken token)
        {
            int c = await SafeReadByteAsync(token).ConfigureAwait(false);
            if (c == -1)
                throw CreateNoMoreDataError();

            var s = await ReadLineAsync(token).ConfigureAwait(false);

            if (log.IsDebugEnabled)
                Log((char)c + s);

            if (c == '-')
                throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);

            return s;
        }

        private async ValueTask<byte[][]> ReadMultiDataAsync(CancellationToken token)
        {
            int c = await SafeReadByteAsync(token).ConfigureAwait(false);
            if (c == -1)
                throw CreateNoMoreDataError();

            var s = await ReadLineAsync(token).ConfigureAwait(false);
            if (log.IsDebugEnabled)
                Log("R: {0}", s);

            switch (c)
            {
                // Some commands like BRPOPLPUSH may return Bulk Reply instead of Multi-bulk
                case '$':
                    var t = new byte[2][];
                    t[1] = await ParseSingleLineAsync(string.Concat(char.ToString((char)c), s), token).ConfigureAwait(false);
                    return t;

                case '-':
                    throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);

                case '*':
                    if (int.TryParse(s, out var count))
                    {
                        if (count == -1)
                        {
                            //redis is in an invalid state
                            return TypeConstants.EmptyByteArrayArray;
                        }

                        var result = new byte[count][];

                        for (int i = 0; i < count; i++)
                            result[i] = await ReadDataAsync(token).ConfigureAwait(false);

                        return result;
                    }
                    break;
            }

            throw CreateResponseError("Unknown reply on multi-request: " + ((char)c) + s); // c here is the protocol prefix
        }

        internal async ValueTask<long> ReadLongAsync(CancellationToken token)
        {
            int c = await SafeReadByteAsync(token).ConfigureAwait(false);
            if (c == -1)
                throw CreateNoMoreDataError();

            return ParseLong(c, await ReadLineAsync(token).ConfigureAwait(false));
        }

        private ValueTask<double> ReadDoubleAsync(CancellationToken token)
            => ReadDataAsync(token).Await(bytes => bytes == null ? double.NaN : ParseDouble(bytes));

        internal ValueTask ExpectOkAsync(CancellationToken token)
            => ExpectWordAsync(OK, token);

        internal ValueTask ExpectQueuedAsync(CancellationToken token)
            => ExpectWordAsync(QUEUED, token);

        internal ValueTask<long> ExpectSuccessFnAsync(CancellationToken token)
        {
            var pending = ExpectSuccessAsync(token);
            return pending.IsCompletedSuccessfully ? default : Awaited(pending);

            static async ValueTask<long> Awaited(ValueTask pending)
            {
                await pending.ConfigureAwait(false);
                return 0;
            }
        }

        internal async ValueTask ExpectSuccessAsync(CancellationToken token)
        {
            int c = await SafeReadByteAsync(token).ConfigureAwait(false);
            if (c == -1)
                throw CreateNoMoreDataError();

            var s = await ReadLineAsync(token).ConfigureAwait(false);

            if (log.IsDebugEnabled)
                Log((char)c + s);

            if (c == '-')
                throw CreateResponseError(s.StartsWith("ERR") && s.Length >= 4 ? s.Substring(4) : s);
        }


        private async ValueTask ExpectWordAsync(string word, CancellationToken token)
        {
            int c = await SafeReadByteAsync(token).ConfigureAwait(false);
            if (c == -1)
                throw CreateNoMoreDataError();

            var s = await ReadLineAsync(token).ConfigureAwait(false);

            if (log.IsDebugEnabled)
                Log((char)c + s);

            if (c == '-')
                throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);

            if (s != word)
                throw CreateResponseError($"Expected '{word}' got '{s}'");
        }

        internal async ValueTask<int> ReadMultiDataResultCountAsync(CancellationToken token)
        {
            int c = await SafeReadByteAsync(token).ConfigureAwait(false);
            if (c == -1)
                throw CreateNoMoreDataError();

            var s = await ReadLineAsync(token).ConfigureAwait(false);
            if (log.IsDebugEnabled)
                Log("R: {0}", s);
            if (c == '-')
                throw CreateResponseError(s.StartsWith("ERR") ? s.Substring(4) : s);
            if (c == '*')
            {
                if (int.TryParse(s, out var count))
                {
                    return count;
                }
            }
            throw CreateResponseError("Unknown reply on multi-request: " + ((char)c) + s); // c here is the protocol prefix
        }
    }
}