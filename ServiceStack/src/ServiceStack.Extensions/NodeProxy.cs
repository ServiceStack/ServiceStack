using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack;

public class NodeProxy
{
    public HttpClient Client { get; set; }
    public bool Verbose { get; set; } = true;
    public string LogPrefix { get; set; } = "[node] ";

    public List<string> CacheFileExtensions { get; set; } = [
        ".js", 
        ".css", 
        ".ico", 
        ".png", 
        ".jpg", 
        ".jpeg", 
        ".gif", 
        ".svg", 
        ".woff", 
        ".woff2", 
        ".ttf", 
        ".eot", 
        ".otf", 
        ".map"
    ];

    public Func<HttpContext, bool> ShouldCache { get; set; }

    public bool DefaultShouldCache(HttpContext context)
    {
        // Ignore if local
        if (context.Request.Host.Value!.Contains("localhost"))
            return false;
        // Ignore Cache-Control headers
        if (context.Request.Headers.TryGetValue("Cache-Control", out var cacheControlValues))
            return false;
        // Ignore if has QueryString
        if (context.Request.QueryString.HasValue)
            return false;

        var path = context.Request.Path.Value ?? string.Empty;
        if (path.Length > 0)
        {
            foreach (var ext in CacheFileExtensions)
            {
                if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public ConcurrentDictionary<string, (string mimeType, byte[] data, string? encoding)> Cache { get; } = new();

    public NodeProxy(HttpClient client)
    {
        Init(client);
    }

    public NodeProxy(string baseUrl, bool ignoreCerts = false)
    {
        // HTTPS not needed when proxying to internally
        HttpMessageHandler nextHandler = ignoreCerts
            ? new HttpClientHandler {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
             }
            : new HttpClientHandler();
        var client = new HttpClient(nextHandler) {
            BaseAddress = new Uri(baseUrl)
        };
        Init(client);
    }

    private void Init(HttpClient client)
    {
        Client = client;
        ShouldCache = DefaultShouldCache;
    }

    public bool TryStartNode(string workingDirectory, out Process process)
    {
        process = new Process 
        {
            StartInfo = new() {
                FileName = "npm",
                Arguments = "run dev",
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }, 
            EnableRaisingEvents = true,
        };
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        if (Verbose)
        {
            var logPrefixTrimmed = LogPrefix.TrimEnd();
            process.OutputDataReceived += (s, e) => {
                if (e.Data != null)
                {
                    if (!string.IsNullOrEmpty(logPrefixTrimmed))
                    {
                        Console.Write(logPrefixTrimmed + ":");
                    }
                    Console.WriteLine(e.Data);
                }
            };
            process.ErrorDataReceived += (s, e) => {
                if (e.Data != null)
                {
                    Console.Write(LogPrefix + "ERROR:");
                    Console.WriteLine(e.Data);
                }
            };
        }
        if (!process.Start())
        {
            return false;
        }
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();        
        return true;
    }

    static bool IsHopByHopHeader(string headerName)
    {
        return headerName.Equals("Connection", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("Keep-Alive", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("Proxy-Connection", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase)
            || headerName.Equals("Upgrade", StringComparison.OrdinalIgnoreCase);
    }

    public async Task HttpToNode(HttpContext context)
    {
        var request = context.Request;

        var cacheKey = request.Path.Value ?? string.Empty;

        // Handle ?clear commands even if this request itself isn't cacheable
        var qs = request.QueryString.Value ?? string.Empty;
        if (qs.Contains("?clear"))
        {
            if (qs.Contains("?clear=all"))
            {
                Cache.Clear();
            }
            else
            {
                Cache.TryRemove(cacheKey, out _);
            }
        }

        var shouldCache = ShouldCache(context);
        if (shouldCache && Cache.TryGetValue(cacheKey, out var cached))
        {
            if (Verbose) Console.WriteLine($"Cache hit: {cacheKey} |mimeType| {cached.mimeType} |encoding| {cached.encoding} |size| {cached.data.Length}");
            context.Response.ContentType = cached.mimeType;
            if (!string.IsNullOrEmpty(cached.encoding))
            {
                context.Response.Headers["Content-Encoding"] = cached.encoding;
            }
            await context.Response.Body.WriteAsync(cached.data, context.RequestAborted);
            return;
        }

        // Build relative URI (path + query)
        var path = request.Path.HasValue ? request.Path.Value : "/";
        var query = request.QueryString.HasValue ? request.QueryString.Value : string.Empty;
        var targetUri = new Uri(path + query, UriKind.Relative);

        using var forwardRequest = new HttpRequestMessage(new HttpMethod(request.Method), targetUri);

        // Copy headers (excluding hop-by-hop headers)
        foreach (var header in request.Headers)
        {
            if (IsHopByHopHeader(header.Key))
                continue;

            if (!forwardRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                forwardRequest.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        // Copy body for non-GET methods
        if (!ServiceStack.HttpMethods.IsGet(request.Method) &&
            !ServiceStack.HttpMethods.IsHead(request.Method) &&
            !ServiceStack.HttpMethods.IsDelete(request.Method) &&
            !ServiceStack.HttpMethods.IsTrace(request.Method))
        {
            forwardRequest.Content = new StreamContent(request.Body);
        }

        using var response = await Client.SendAsync(
            forwardRequest,
            HttpCompletionOption.ResponseHeadersRead,
            context.RequestAborted);

        context.Response.StatusCode = (int)response.StatusCode;
        foreach (var header in response.Headers)
        {
            if (IsHopByHopHeader(header.Key))
                continue;

            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
        foreach (var header in response.Content.Headers)
        {
            if (IsHopByHopHeader(header.Key))
                continue;

            context.Response.Headers[header.Key] = header.Value.ToArray();
        }

        // ASP.NET Core will set its own transfer-encoding
        context.Response.Headers.Remove("transfer-encoding");

        if (context.Response.StatusCode == StatusCodes.Status200OK)
        {
            if (shouldCache)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                if (bytes.Length > 0)
                {
                    var mimeType = response.Content.Headers.ContentType?.ToString()
                        ?? ServiceStack.MimeTypes.GetMimeType(cacheKey);
                    var encoding = response.Content.Headers.ContentEncoding.FirstOrDefault();
                    Cache[cacheKey] = (mimeType, bytes, encoding);
                    if (Verbose) Console.WriteLine($"Cache miss: {cacheKey} |mimeType| {mimeType} |encoding| {encoding} |size| {bytes.Length}");
                    await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
                    return;
                }
            }
        }

        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
}

public static class ProxyExtensions
{

    /// <summary>
    /// Proxy 404s to Node.js (except for API/backend routes) must be registered before endpoints
    /// </summary>
    public static void MapNotFoundToNode(this WebApplication app, NodeProxy proxy, string[]? ignorePaths=null)
    {
        app.Use(async (context, next) =>
        {
            await next();

            if (context.Response.StatusCode == StatusCodes.Status404NotFound &&
                !context.Response.HasStarted)
            {
                var pathValue = context.Request.Path.Value ?? string.Empty;

                // Keep backend/api/identity/swagger/auth 404s as-is
                if (ignorePaths != null && ignorePaths.Any(x => pathValue.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                // Clear the 404 and let Next handle it
                context.Response.Clear();
                await proxy.HttpToNode(context);
            }
        });
    }

    /// <summary>
    /// Map clean URLs to .html files
    /// </summary>
    public static void MapCleanUrls(this WebApplication app)
    {
        // Serve .html files without extension
        app.Use(async (context, next) =>
        {
            // Only process GET requests that don't have an extension and don't start with /api
            var path = context.Request.Path.Value;
            if (context.Request.Method == "GET" && !string.IsNullOrEmpty(path) && !Path.HasExtension(path)
                && !path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
            {
                var fileProvider = app.Environment.WebRootFileProvider;
                var fileInfo = fileProvider.GetFileInfo(path + ".html");
                if (fileInfo.Exists && !fileInfo.IsDirectory)
                {
                    context.Response.ContentType = "text/html";
                    using var stream = fileInfo.CreateReadStream();
                    await stream.CopyToAsync(context.Response.Body); // Serve the HTML file directly
                    return; // Don't call next(), we've handled the request
                }
            }
            await next();
        });
    }
    
    /// <summary>
    /// Map Next.js HMR WebSocket requests
    /// </summary>
    public static IEndpointConventionBuilder MapNextHmr(this WebApplication app, NodeProxy proxy)
    {
        return app.Map("/_next/webpack-hmr", async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                await WebSocketToNode(context, proxy.Client.BaseAddress!);
            }
            else
            {
                // HMR endpoint expects WebSocket connections only
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("WebSocket connection expected");
            }
        });
    }

    /// <summary>
    /// Proxy WebSocket requests to Node.js
    /// </summary>
    public static async Task WebSocketToNode(HttpContext context, Uri nextServerBase, bool allowInvalidCerts=true)
    {
        using var clientSocket = await context.WebSockets.AcceptWebSocketAsync();

        using var nextSocket = new ClientWebSocket();
        if (allowInvalidCerts && nextServerBase.Scheme == "https")
        {
            nextSocket.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        }

        if (context.Request.Headers.TryGetValue("Cookie", out var cookieValues))
        {
            nextSocket.Options.SetRequestHeader("Cookie", cookieValues.ToString());
        }

        var builder = new UriBuilder(nextServerBase)
        {
            Scheme = nextServerBase.Scheme == "https" ? "wss" : "ws",
            Path = context.Request.Path.HasValue ? context.Request.Path.Value : "/",
            Query = context.Request.QueryString.HasValue
                ? context.Request.QueryString.Value!.TrimStart('?')
                : string.Empty
        };

        await nextSocket.ConnectAsync(builder.Uri, context.RequestAborted);

        var forwardTask = PumpWebSocket(clientSocket, nextSocket,  context.RequestAborted);
        var reverseTask = PumpWebSocket(nextSocket, clientSocket, context.RequestAborted);

        await Task.WhenAll(forwardTask, reverseTask);
    }

    static async Task PumpWebSocket(
        WebSocket source,
        WebSocket destination,
        CancellationToken cancellationToken=default)
    {
        try
        {
            var buffer = new byte[8192];

            while (source.State == WebSocketState.Open &&
                destination.State == WebSocketState.Open)
            {
                var result = await source.ReceiveAsync(
                    new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await destination.CloseAsync(
                        source.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                        source.CloseStatusDescription,
                        cancellationToken);
                    break;
                }

                await destination.SendAsync(
                    new ArraySegment<byte>(buffer, 0, result.Count),
                    result.MessageType,
                    result.EndOfMessage,
                    cancellationToken);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"WebSocket Proxy: {e.Message}");
        }
    }

    /// <summary>
    /// Run Next.js dev server if not already running
    /// </summary>
    public static System.Diagnostics.Process? RunNodeProcess(this WebApplication app,
        NodeProxy proxy,
        string lockFile,
        string workingDirectory, 
        bool registerExitHandler=true)
    {
        var process = app.StartNodeProcess(proxy, lockFile, workingDirectory, registerExitHandler);
        var logPrefix = proxy.LogPrefix;
        if (process != null)
        {
            Console.WriteLine(logPrefix + "Started Next.js dev server");
        }
        else
        {
            Console.WriteLine(logPrefix + "Next.js dev server already running");
        }
        return process;
    }

    public static System.Diagnostics.Process? StartNodeProcess(this WebApplication app, 
        NodeProxy proxy,
        string lockFile,
        string workingDirectory, 
        bool registerExitHandler=true)
    {
        if (!File.Exists(lockFile))
        {
            if (!proxy.TryStartNode(workingDirectory, out var process))
                return null;

            var verbose = proxy.Verbose;
            var logPrefix = string.IsNullOrEmpty(proxy.LogPrefix) ? "" : proxy.LogPrefix + " ";
            process.Exited += (s, e) => {
                if (verbose) Console.WriteLine(logPrefix + "Exited: " + process.ExitCode);
                File.Delete(lockFile);
            };

            if (registerExitHandler)
            {
                app.Lifetime.ApplicationStopping.Register(() => {
                    if (!process.HasExited)
                    {
                        if (verbose) Console.WriteLine(logPrefix + "Teminating process: " + process.Id);
                        process.Kill(entireProcessTree: true);
                    }
                });
            }
            return process;
        }

        return null;
    }
    
    public static IEndpointConventionBuilder MapFallbackToNode(this WebApplication app, NodeProxy proxy)
    {
        return app.MapFallback(proxy.HttpToNode);
    }
}
