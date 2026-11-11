using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Redis;

public partial class RedisNativeClient
{
    /// <summary>Optional asynchronous initialization, run after the existing connection hooks on async connections.</summary>
    public Func<IRedisNativeClientAsync, CancellationToken, ValueTask> ConnectionFilterAsync { get; set; }

    /// <summary>Open this exclusive client's connection asynchronously, if needed. ConnectTimeout covers DNS through initialization.</summary>
    public async ValueTask ConnectAsync(CancellationToken token = default)
        => await TryConnectIfNeededAsync(token).ConfigureAwait(false);

    private async ValueTask<bool> TryConnectIfNeededAsync(CancellationToken token)
    {
        if (IsDisposed) throw new ObjectDisposedException($"Redis Client {ClientId} is Disposed");
        token.ThrowIfCancellationRequested();
        if (LastConnectedAtTimestamp > 0)
        {
            var now = Stopwatch.GetTimestamp();
            var elapsedSecs = (now - LastConnectedAtTimestamp) / Stopwatch.Frequency;
            if (socket == null || (elapsedSecs > IdleTimeOutSecs && !socket.IsConnected()))
                SafeConnectionClose();
            LastConnectedAtTimestamp = now;
        }
        if (socket != null) return false;
        await OpenConnectionAsync(token).ConfigureAwait(false);
        return true;
    }

    private async ValueTask OpenConnectionAsync(CancellationToken token)
    {
        InitUsageTimer();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        if (ConnectTimeout > 0) deadline.CancelAfter(ConnectTimeout);
        var setupTimer = Stopwatch.StartNew();
        var useToken = deadline.Token;
        var id = Diagnostics.Redis.WriteConnectionOpenBefore(this);
        Exception error = null;
        try
        {
            // The proxy compatibility reconnect below shares this deadline, including DNS and TLS.
            while (true)
            {
                useToken.ThrowIfCancellationRequested();
                var addresses = IPAddress.TryParse(Host, out var literal)
                    ? [literal]
#if NET8_0_OR_GREATER
                    : await Dns.GetHostAddressesAsync(Host, useToken).ConfigureAwait(false);
#else
                    : await AwaitDnsAsync(Dns.GetHostAddressesAsync(Host), useToken).ConfigureAwait(false);
#endif
                addresses = PreferIPv4(addresses);
                SocketException lastError = null;
                for (var i = 0; i < addresses.Length; i++)
                {
                    useToken.ThrowIfCancellationRequested();
                    var candidate = new Socket(addresses[i].AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                    {
                        SendTimeout = SendTimeout,
                        ReceiveTimeout = ReceiveTimeout
                    };
                    socket = candidate;
                    // Dual-stack hosts: give every attempt but the last a share of the remaining ConnectTimeout,
                    // so an unreachable first address (e.g. blackholed IPv6) can't consume the whole budget.
                    using var attempt = CancellationTokenSource.CreateLinkedTokenSource(useToken);
                    var remainingAddresses = addresses.Length - i;
                    if (ConnectTimeout > 0 && remainingAddresses > 1)
                    {
                        var budget = ConnectTimeout - setupTimer.ElapsedMilliseconds;
                        attempt.CancelAfter((int)Math.Max(1, budget / remainingAddresses));
                    }
                    var attemptToken = attempt.Token;
                    // Closing the captured socket also interrupts framework APIs without cancellation overloads.
                    using var cancellation = attemptToken.Register(() => { try { candidate.Dispose(); } catch { } });
                    try
                    {
#if NET6_0_OR_GREATER
                        await candidate.ConnectAsync(addresses[i], Port, attemptToken).ConfigureAwait(false);
#else
                        await candidate.ConnectAsync(addresses[i], Port).ConfigureAwait(false);
#endif
                        lastError = null;
                        break;
                    }
                    catch (Exception ex) when (ex is SocketException
                        || (attemptToken.IsCancellationRequested && !useToken.IsCancellationRequested))
                    {
                        candidate.Dispose();
                        socket = null;
                        useToken.ThrowIfCancellationRequested();
                        lastError = ex as SocketException ?? new SocketException((int)SocketError.TimedOut);
                    }
                }
                if (socket == null) throw lastError ?? new SocketException((int)SocketError.HostNotFound);
                var connectedSocket = socket;
                using var closeOnCancellation = useToken.Register(() => { try { connectedSocket.Dispose(); } catch { } });
                Stream stream = new NetworkStream(connectedSocket);
                if (Ssl)
                {
                    sslStream = new SslStream(stream, false, RedisConfig.CertificateValidationCallback,
                        RedisConfig.CertificateSelectionCallback, EncryptionPolicy.RequireEncryption);
                    await sslStream.AuthenticateAsClientAsync(Host, new X509CertificateCollection(),
                        SslProtocols ?? System.Security.Authentication.SslProtocols.None,
                        checkCertificateRevocation: SslProtocols != null).ConfigureAwait(false);
                    if (!sslStream.IsEncrypted)
                        throw new AuthenticationException($"Could not establish an encrypted connection to '{Host}:{Port}'");
                    stream = sslStream;
                }
                bufferedReader = new BufferedReader(stream, 16 * 1024);
                if (!string.IsNullOrEmpty(Password))
                {
                    await SendConnectionCommandAsync(stream, useToken, !string.IsNullOrEmpty(Username)
                        ? [Commands.Auth, Username.ToUtf8Bytes(), Password.ToUtf8Bytes()]
                        : [Commands.Auth, Password.ToUtf8Bytes()]).ConfigureAwait(false);
                    await ExpectWordAsync(OK, useToken).ConfigureAwait(false);
                }
                if (db != 0)
                {
                    await SendConnectionCommandAsync(stream, useToken, Commands.Select, db.ToUtf8Bytes()).ConfigureAwait(false);
                    await ExpectWordAsync(OK, useToken).ConfigureAwait(false);
                }
                if (Client != null)
                {
                    await SendConnectionCommandAsync(stream, useToken, Commands.Client, Commands.SetName, Client.ToUtf8Bytes()).ConfigureAwait(false);
                    await ExpectWordAsync(OK, useToken).ConfigureAwait(false);
                }
                if (ServerVersionNumber == 0)
                {
                    ServerVersionNumber = RedisConfig.AssumeServerVersion.GetValueOrDefault();
                    if (ServerVersionNumber <= 0)
                    {
                        try
                        {
                            TrackCommandUsage();
                            await SendConnectionCommandAsync(stream, useToken, Commands.Info).ConfigureAwait(false);
                            var info = ParseInfoResult((await ReadDataAsync(useToken).ConfigureAwait(false)).FromUtf8Bytes());
                            Interlocked.Increment(ref RedisState.TotalCommandsSent);
                            info.TryGetValue("redis_version", out var version);
                            ServerVersionNumber = ParseServerVersionNumber(version);
                        }
                        catch
                        {
                            useToken.ThrowIfCancellationRequested();
                            // Preserve Twemproxy/INFO-denied compatibility; never fall back after cancellation.
                            ServerVersionNumber = Unknown;
                            SafeConnectionClose();
                            continue;
                        }
                    }
                }
                clientPort = connectedSocket.LocalEndPoint is IPEndPoint endpoint ? endpoint.Port : -1;
                lastCommand = null;
                lastSocketException = null;
                LastConnectedAtTimestamp = Stopwatch.GetTimestamp();
                useToken.ThrowIfCancellationRequested();
#if DEBUG
                var oldDebugAllowSync = DebugAllowSync;
                DebugAllowSync = true; // Existing user hooks retain their synchronous behavior.
                try
                {
#endif
                    OnConnected();
                    ConnectionFilter?.Invoke(this);
#if DEBUG
                }
                finally { DebugAllowSync = oldDebugAllowSync; }
#endif
                if (ConnectionFilterAsync != null)
                    await ConnectionFilterAsync((IRedisNativeClientAsync)this, useToken).ConfigureAwait(false);
                useToken.ThrowIfCancellationRequested();
                return;
            }
        }
        catch (Exception ex)
        {
            error = ex;
            DeactivatedAt = DateTime.UtcNow;
            SafeConnectionClose();
            ResetSendBuffer();
            if (token.IsCancellationRequested)
                throw (error = new OperationCanceledException(token));
            if (deadline.IsCancellationRequested)
                throw (error = new TimeoutException($"Redis connection setup to '{Host}:{Port}' exceeded ConnectTimeout {ConnectTimeout}ms", ex));
            throw;
        }
        finally
        {
            if (error != null) Diagnostics.Redis.WriteConnectionOpenError(id, this, error);
            else Diagnostics.Redis.WriteConnectionOpenAfter(id, this);
        }
    }

    // Cleanup of async resolution/creation must not issue a synchronous QUIT on an uncertain connection.
    internal void AbortConnection()
    {
        SafeConnectionClose();
        ResetSendBuffer();
    }

    internal CancellationTokenRegistration CloseSocketOnCancellation(CancellationToken token)
    {
        var current = socket;
        return token.Register(() => { try { current?.Dispose(); } catch { } });
    }

    // Separate from the command/pipeline buffer: connection setup must not flush queued application commands.
    private async ValueTask SendConnectionCommandAsync(Stream stream, CancellationToken token, params byte[][] args)
    {
        using var buffer = new MemoryStream();
        var prefix = GetCmdBytes('*', args.Length);
        buffer.Write(prefix, 0, prefix.Length);
        foreach (var arg in args)
        {
            prefix = GetCmdBytes('$', arg.Length);
            buffer.Write(prefix, 0, prefix.Length);
            buffer.Write(arg, 0, arg.Length);
            buffer.Write(endData, 0, endData.Length);
        }
        var bytes = buffer.ToArray();
        await stream.WriteAsync(bytes, 0, bytes.Length, token).ConfigureAwait(false);
    }

#if !NET8_0_OR_GREATER
    private static async Task<IPAddress[]> AwaitDnsAsync(Task<IPAddress[]> lookup, CancellationToken token)
    {
        var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = token.Register(() => cancelled.TrySetResult(true));
        if (await Task.WhenAny(lookup, cancelled.Task).ConfigureAwait(false) != lookup)
        {
            // Legacy DNS cannot be stopped, but owns no socket/client state and late failures are observed.
            _ = lookup.ContinueWith(t => { _ = t.Exception; }, CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            token.ThrowIfCancellationRequested();
        }
        return await lookup.ConfigureAwait(false);
    }
#endif

    protected async ValueTask<int> AssertServerVersionNumberAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (ServerVersionNumber == 0)
        {
            await TryConnectIfNeededAsync(token).ConfigureAwait(false);
            if (ServerVersionNumber == 0 && Pipeline == null && Transaction == null)
            {
                try
                {
                    var info = await ((IRedisNativeClientAsync)this).InfoAsync(token).ConfigureAwait(false);
                    info.TryGetValue("redis_version", out var version);
                    ServerVersionNumber = ParseServerVersionNumber(version);
                }
                catch (RedisException) { token.ThrowIfCancellationRequested(); ServerVersionNumber = Unknown; }
            }
        }
        return ServerVersionNumber;
    }
}
