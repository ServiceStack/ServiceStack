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

namespace ServiceStack.Redis;

partial class RedisNativeClient
{
    private async ValueTask<byte[][]> SendExpectMultiDataAsync(CancellationToken token, params byte[][] cmdWithBinaryArgs)
    {
        return (await SendReceiveAsync(cmdWithBinaryArgs, ReadMultiDataAsync, token,
                   PipelineAsync != null ? PipelineAsync.CompleteMultiBytesQueuedCommandAsync : null).ConfigureAwait(false))
               ?? TypeConstants.EmptyByteArrayArray;
    }

    protected ValueTask SendWithoutReadAsync(CancellationToken token, params byte[][] cmdWithBinaryArgs)
        => SendReceiveAsync<long>(cmdWithBinaryArgs, null, token, null, sendWithoutRead: true).Await();

    private ValueTask<long> SendExpectLongAsync(CancellationToken token, params byte[][] cmdWithBinaryArgs)
    {
        return SendReceiveAsync(cmdWithBinaryArgs, ReadLongAsync, token,
            PipelineAsync != null ? PipelineAsync.CompleteLongQueuedCommandAsync : null);
    }

    private ValueTask<double> SendExpectDoubleAsync(CancellationToken token, params byte[][] cmdWithBinaryArgs)
    {
        return SendReceiveAsync(cmdWithBinaryArgs, ReadDoubleAsync, token,
            PipelineAsync != null ? PipelineAsync.CompleteDoubleQueuedCommandAsync : null);
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
        return SendReceiveAsync(cmdWithBinaryArgs, ReadDataAsync, token, PipelineAsync != null ? PipelineAsync.CompleteBytesQueuedCommandAsync : null);
    }

    private ValueTask<string> SendExpectCodeAsync(CancellationToken token, params byte[][] cmdWithBinaryArgs)
    {
        return SendReceiveAsync(cmdWithBinaryArgs, ExpectCodeAsync, token, PipelineAsync != null ? PipelineAsync.CompleteStringQueuedCommandAsync : null);
    }

    private ValueTask<ScanResult> SendExpectScanResultAsync(CancellationToken token, byte[] cmd, params byte[][] args)
    {
        var cmdWithArgs = MergeCommandWithArgs(cmd, args);
        return SendExpectDeeplyNestedMultiDataAsync(token, cmdWithArgs).Await(ParseScanResult);
    }

    private ValueTask<object[]> SendExpectDeeplyNestedMultiDataAsync(CancellationToken token, params byte[][] cmdWithBinaryArgs)
        => SendReceiveAsync(cmdWithBinaryArgs, ReadDeeplyNestedMultiDataAsync, token);

    private ValueTask<object[]> ReadDeeplyNestedMultiDataAsync(CancellationToken token)
        => ReadDeeplyNestedMultiDataItemAsync(token).Await(result => (object[])result);

    private async ValueTask<object> ReadDeeplyNestedMultiDataItemAsync(CancellationToken token)
    {
        int c = await SafeReadByteAsync(token).ConfigureAwait(false);
        if (c == -1)
            throw CreateAsyncNoMoreDataError();

        var s = await ReadLineAsync(token).ConfigureAwait(false);
        if (log.IsDebugEnabled)
            Log("R: {0}", s);

        switch (c)
        {
            case '$':
                return await ParseSingleLineAsync(string.Concat(char.ToString((char)c), s), token).ConfigureAwait(false);

            case '-':
                throw CreateResponseError(StripErrPrefix(s));

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
            throw CreateAsyncNoMoreDataError();

        var s = await ReadLineAsync(token).ConfigureAwait(false);
        if (log.IsDebugEnabled)
            Log("R: {0}", s);

        switch (c)
        {
            case '$':
                var bytes = await ParseSingleLineAsync(string.Concat(char.ToString((char)c), s), token).ConfigureAwait(false);
                return new RedisData { Data = bytes, IsNull = bytes == null };

            case '-':
                throw CreateResponseError(StripErrPrefix(s));

            case '*':
                if (int.TryParse(s, out var count))
                {
                    var ret = new RedisData { Children = new List<RedisData>(), IsNull = count == -1 };
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
        bool sendWithoutRead = false, [CallerMemberName] string operation = "")
    {
        //if (TrackThread != null)
        //{
        //    if (TrackThread.Value.ThreadId != Thread.CurrentThread.ManagedThreadId)
        //        throw new InvalidAccessException(TrackThread.Value.ThreadId, TrackThread.Value.StackTrace);
        //}

        var i = 0;
        var didWriteToBuffer = false;
        Exception originalEx = null;
        Exception wasError = null;
        Guid id = Guid.Empty; 

        var firstAttempt = DateTime.UtcNow;

        while (true)
        {
            var useToken = token;
            var linkedCts = SendTimeout > 0 
                ? CancellationTokenSource.CreateLinkedTokenSource(token) 
                : null;
            if (linkedCts != null)
            {
                linkedCts.CancelAfter(SendTimeout);
                useToken = linkedCts.Token;
            }
        
            // this is deliberately *before* the try, so we never retry
            // if we've been cancelled
            useToken.ThrowIfCancellationRequested();
            try
            {
                if (await TryConnectIfNeededAsync(useToken).ConfigureAwait(false))
                    didWriteToBuffer = false;

                if (socket == null)
                    throw new RedisRetryableException("Socket is not connected");

                if (!didWriteToBuffer) //only write to buffer once
                {
                    id = Diagnostics.Redis.WriteCommandBefore(cmdWithBinaryArgs, operation);
                    WriteCommandToSendBuffer(cmdWithBinaryArgs);
                    didWriteToBuffer = true;
                }

                if (PipelineAsync == null) //pipeline will handle flush if in pipeline
                {
                    await FlushSendBufferAsync(useToken).ConfigureAwait(false);
                }
                else if (!sendWithoutRead)
                {
                    if (completePipelineFn == null)
                        throw new NotSupportedException("Pipeline is not supported.");

                    completePipelineFn(fn);
                    return default;
                }

                if (i > 0)
                {
                    Interlocked.Increment(ref RedisState.TotalRetrySuccess);
                    Diagnostics.Redis.WriteCommandRetry(id, cmdWithBinaryArgs, operation);
                }

                var result = default(T);
                if (fn != null)
                {
                    var recvToken = token;
                    var linkedRecvCts = ReceiveTimeout > 0 
                        ? CancellationTokenSource.CreateLinkedTokenSource(token) 
                        : null;
                    if (linkedRecvCts != null)
                    {
                        linkedRecvCts.CancelAfter(ReceiveTimeout);
                        recvToken = linkedRecvCts.Token;
                    }
                    try
                    {
                        result = await fn(recvToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        linkedRecvCts?.Dispose();
                    }
                }

                if (Pipeline == null)
                    ResetSendBuffer();

                Interlocked.Increment(ref RedisState.TotalCommandsSent);

                return result;
            }
            catch (Exception outerEx)
            {
                wasError = outerEx;
                if (log.IsDebugEnabled)
                    logDebug("SendReceive Exception: " + outerEx.Message);

                if (outerEx is OperationCanceledException || outerEx is TimeoutException || outerEx is ObjectDisposedException)
                {
                    DeactivatedAt = DateTime.UtcNow;
                    SafeConnectionClose();
                    ResetSendBuffer();
                    token.ThrowIfCancellationRequested();
                    throw;
                }

                var retryableEx = outerEx as RedisRetryableException;
                if (retryableEx == null && outerEx is RedisException
                    || outerEx is LicenseException)
                {
                    ResetSendBuffer();
                    throw;
                }

                var ex = retryableEx ?? GetRetryableException(outerEx);
                originalEx ??= ex ?? throw CreateConnectionError(originalEx ?? outerEx);

                var retry = DateTime.UtcNow - firstAttempt < retryTimeout;
                if (!retry)
                {
                    if (Pipeline == null)
                        ResetSendBuffer();

                    Interlocked.Increment(ref RedisState.TotalRetryTimedout);
                    throw CreateRetryTimeoutException(retryTimeout, originalEx);
                }

                Interlocked.Increment(ref RedisState.TotalRetryCount);
                await Task.Delay(GetBackOffMultiplier(++i), useToken).ConfigureAwait(false);
                wasError = null;
            }
            finally
            {
                if (wasError != null)
                    Diagnostics.Redis.WriteCommandError(id, cmdWithBinaryArgs, originalEx ?? wasError, operation);
                else
                    Diagnostics.Redis.WriteCommandAfter(id, cmdWithBinaryArgs, operation);
                
                linkedCts?.Dispose();
            }
        }
    }

    internal async ValueTask FlushSendBufferAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (currentBufferIndex > 0) PushCurrentBuffer();
        if (cmdBuffer.Count == 0) return;
        OnBeforeFlush?.Invoke();
        token.ThrowIfCancellationRequested();
        using var cancellation = CloseSocketOnCancellation(token);
        try
        {
            if (sslStream != null)
            {
                foreach (var segment in cmdBuffer)
                {
#if NET6_0_OR_GREATER
                    await sslStream.WriteAsync(new ReadOnlyMemory<byte>(segment.Array, segment.Offset, segment.Count), token).ConfigureAwait(false);
#else
                    await sslStream.WriteAsync(segment.Array, segment.Offset, segment.Count, token).ConfigureAwait(false);
#endif
                }
            }
            else if (Env.IsMono)
            {
                foreach (var segment in cmdBuffer)
                {
                    var sent = 0;
                    while (sent < segment.Count)
                    {
                        var count = await socket.SendAsync(new ArraySegment<byte>(segment.Array,
                            segment.Offset + sent, segment.Count - sent), SocketFlags.None).ConfigureAwait(false);
                        if (count <= 0) throw new IOException("Redis socket closed while sending a command.");
                        sent += count;
                    }
                }
            }
            else
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
                    logDebug("socket.Send: " + StringBuilderCache.ReturnAndFree(sb.Replace("\r\n", " ")).SafeSubstring(0,50));
                }

                IList<ArraySegment<byte>> pending = cmdBuffer;
                do
                {
                    var sent = await socket.SendAsync(pending, SocketFlags.None).ConfigureAwait(false);
                    pending = RemainingSendBuffers(pending, sent);
                } while (pending != null);
            }
        }
        catch
        {
            token.ThrowIfCancellationRequested();
            throw;
        }
    }

    private ValueTask<int> SafeReadByteAsync(in CancellationToken token, [CallerMemberName]string name = null)
    {
        AssertNotDisposed();

        if (log.IsDebugEnabled && RedisConfig.EnableVerboseLogging)
            logDebug(name + "()");

        return bufferedReader.ReadByteAsync(token);
    }

    private RedisRetryableException CreateAsyncNoMoreDataError()
    {
        // A parser owns no reconnection policy. The async command loop may retry with its own token.
        AbortConnection();
        return new RedisRetryableException("Redis connection closed before a complete response.");
    }

    private async ValueTask<string> ReadLineAsync(CancellationToken token)
    {
        var sb = StringBuilderCache.Allocate();
        try
        {
            while (true)
            {
                var value = await bufferedReader.ReadByteAsync(token).ConfigureAwait(false);
                if (value == -1) throw CreateAsyncNoMoreDataError();
                if (value == '\r')
                {
                    if (await bufferedReader.ReadByteAsync(token).ConfigureAwait(false) != '\n')
                        throw new System.IO.IOException("Invalid Redis response terminator.");
                    return sb.ToString();
                }
                if (value == '\n') throw new System.IO.IOException("Invalid Redis response terminator.");
                sb.Append((char)value);
            }
        }
        finally { StringBuilderCache.Free(sb); }
    }

    private async ValueTask<byte[]> ParseSingleLineAsync(string r, CancellationToken token)
    {
        if (log.IsDebugEnabled)
            Log("R: {0}", r);
        if (r.Length == 0)
            throw new IOException("Zero length response");

        char c = r[0];
        if (c == '-')
            throw CreateResponseError(StripErrPrefix(r.Substring(1)));

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
                        throw new IOException("Unexpected end of Stream");

                    offset += readCount;
                    count -= readCount;
                }

                if (await bufferedReader.ReadByteAsync(token).ConfigureAwait(false) != '\r'
                    || await bufferedReader.ReadByteAsync(token).ConfigureAwait(false) != '\n')
                    throw new IOException("Invalid termination");

                return retbuf;
            }
            throw new IOException("Invalid length");
        }

        if (c is ':' or '+')
        {
            //match the return value
            return r.Substring(1).ToUtf8Bytes();
        }

        if (r == "*0")
            return Array.Empty<byte>();
            
        throw new IOException("Unexpected reply: " + r);
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
            throw CreateAsyncNoMoreDataError();

        var s = await ReadLineAsync(token).ConfigureAwait(false);

        if (log.IsDebugEnabled)
            Log((char)c + s);

        if (c == '-')
            throw CreateResponseError(StripErrPrefix(s));

        return s;
    }

    private async ValueTask<byte[][]> ReadMultiDataAsync(CancellationToken token)
    {
        int c = await SafeReadByteAsync(token).ConfigureAwait(false);
        if (c == -1)
            throw CreateAsyncNoMoreDataError();

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
                throw CreateResponseError(StripErrPrefix(s));

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
            throw CreateAsyncNoMoreDataError();

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
            throw CreateAsyncNoMoreDataError();

        var s = await ReadLineAsync(token).ConfigureAwait(false);

        if (log.IsDebugEnabled)
            Log((char)c + s);

        if (c == '-')
            throw CreateResponseError(StripErrPrefix(s));
    }


    private async ValueTask ExpectWordAsync(string word, CancellationToken token)
    {
        int c = await SafeReadByteAsync(token).ConfigureAwait(false);
        if (c == -1)
            throw CreateAsyncNoMoreDataError();

        var s = await ReadLineAsync(token).ConfigureAwait(false);

        if (log.IsDebugEnabled)
            Log((char)c + s);

        if (c == '-')
            throw CreateResponseError(StripErrPrefix(s));

        if (s != word)
            throw CreateResponseError($"Expected '{word}' got '{s}'");
    }

    internal async ValueTask<int> ReadMultiDataResultCountAsync(CancellationToken token)
    {
        int c = await SafeReadByteAsync(token).ConfigureAwait(false);
        if (c == -1)
            throw CreateAsyncNoMoreDataError();

        var s = await ReadLineAsync(token).ConfigureAwait(false);
        if (log.IsDebugEnabled)
            Log("R: {0}", s);
        if (c == '-')
            throw CreateResponseError(StripErrPrefix(s));
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