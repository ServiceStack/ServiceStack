using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ServiceStack;

public class NodeProxy
{
    public HttpClient Client { get; set; }
    public ILogger? Log { get; set; }
    
    public string ProcessFileName { get; set; }
    public string ProcessArguments { get; set; }
    public Action<Process>? ConfigureProcess { get; set; }
    public Action<Process>? ConfigureLinuxProcess { get; set; }
    public Action<Process>? ConfigureMacProcess { get; set; }
    public Action<Process>? ConfigureWindowsProcess { get; set; }

    /// <summary>
    /// Maximum size in bytes for an individual file to be cached (default: 5 MB)
    /// </summary>
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024; // 5 MB

    /// <summary>
    /// Maximum total cache size in bytes (default: 100 MB)
    /// </summary>
    public long MaxCacheSizeBytes { get; set; } = 100 * 1024 * 1024; // 100 MB

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

    private class CacheEntry(string mimeType, byte[] data, string? encoding, DateTime lastAccessTime)
    {
        public string MimeType { get; } = mimeType;
        public byte[] Data { get; } = data;
        public string? Encoding { get; } = encoding;
        public DateTime LastAccessTime { get; set; } = lastAccessTime;
        public long Size => Data.Length;
    }

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private long _totalCacheSize = 0;
    private long _cacheHits = 0;
    private long _cacheMisses = 0;
    private readonly object _cacheLock = new();

    public ConcurrentDictionary<string, (string mimeType, byte[] data, string? encoding)> Cache
    {
        get
        {
            // Provide backward compatibility by converting internal cache to old format
            var result = new ConcurrentDictionary<string, (string mimeType, byte[] data, string? encoding)>();
            foreach (var kvp in _cache)
            {
                result[kvp.Key] = (kvp.Value.MimeType, kvp.Value.Data, kvp.Value.Encoding);
            }
            return result;
        }
    }

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

        // On Windows, npm is a batch file, so we need to use cmd.exe
        var isWindows = OperatingSystem.IsWindows();
        ProcessFileName = isWindows ? "cmd.exe" : "npm";
        ProcessArguments = isWindows ? "/c npm run dev" : "run dev";
    }
    
    public bool LogDebug => Log != null && Log.IsEnabled(LogLevel.Debug);

    private void AddToCache(string key, CacheEntry entry)
    {
        lock (_cacheLock)
        {
            // Check if we need to evict entries to make room
            var newSize = _totalCacheSize + entry.Size;

            // If adding this entry exceeds the limit, evict LRU entries
            while (newSize > MaxCacheSizeBytes && _cache.Count > 0)
            {
                // Find the least recently used entry
                var lruKey = _cache.OrderBy(x => x.Value.LastAccessTime).First().Key;
                if (_cache.TryRemove(lruKey, out var removed))
                {
                    _totalCacheSize -= removed.Size;
                    newSize = _totalCacheSize + entry.Size;
                    Log?.LogInformation("Cache evicted (LRU): {LruKey} |size| {Size}", lruKey, removed.Size);
                }
            }

            // Only add if it fits within the total cache limit
            if (entry.Size <= MaxCacheSizeBytes)
            {
                // Remove old entry if updating
                if (_cache.TryRemove(key, out var oldEntry))
                {
                    _totalCacheSize -= oldEntry.Size;
                }

                _cache[key] = entry;
                _totalCacheSize += entry.Size;
            }
            else
            {
                Log?.LogInformation("Cache skip (exceeds total limit): {Key} |size| {Size} |limit| {MaxCacheSizeBytes}",
                    key, entry.Size, MaxCacheSizeBytes);
            }
        }
    }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public (long hits, long misses, double hitRate, int entryCount, long totalSize) GetCacheStats()
    {
        var hits = Interlocked.Read(ref _cacheHits);
        var misses = Interlocked.Read(ref _cacheMisses);
        var total = hits + misses;
        var hitRate = total > 0 ? (double)hits / total : 0.0;

        lock (_cacheLock)
        {
            return (hits, misses, hitRate, _cache.Count, _totalCacheSize);
        }
    }

    /// <summary>
    /// Clear all cache entries
    /// </summary>
    public void ClearCache()
    {
        lock (_cacheLock)
        {
            _cache.Clear();
            _totalCacheSize = 0;
        }
    }

    /// <summary>
    /// Remove a specific cache entry
    /// </summary>
    public bool RemoveCacheEntry(string key)
    {
        lock (_cacheLock)
        {
            if (_cache.TryRemove(key, out var entry))
            {
                _totalCacheSize -= entry.Size;
                return true;
            }
            return false;
        }
    }
    
    public bool IsPortAvailable() => HostContext.IsPortAvailable(Client.BaseAddress!.Port);

    public bool WaitUntilAvailable(TimeSpan timeout)
    {
        var baseUrl = Client.BaseAddress!.ToString();
        var startedAt = DateTime.UtcNow;
        while (DateTime.UtcNow - startedAt < timeout)
        {
            try
            {
                var response = baseUrl.GetStringFromUrl();
                return true;
            }
            catch (Exception ex)
            {
                Thread.Sleep(200);
            }
        }
        return false;
    }

    public bool TryStartNode(string workingDirectory, out Process process)
    {
        // Convert relative path to absolute path for Windows compatibility
        var absoluteWorkingDir = Path.GetFullPath(workingDirectory);
       
        process = new Process
        {
            StartInfo = new() {
                FileName = ProcessFileName,
                Arguments = ProcessArguments,
                WorkingDirectory = absoluteWorkingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true,
        };
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.OutputDataReceived += (s, e) => {
            if (e.Data != null)
            {
                Log?.LogDebug(e.Data);
            }
        };
        process.ErrorDataReceived += (s, e) => {
            if (e.Data != null)
            {
                Log?.LogError(e.Data);
            }
        };
       
        try
        {
            if (ConfigureProcess != null)
            {
                ConfigureProcess(process);
            }
            if (ConfigureWindowsProcess != null && OperatingSystem.IsWindows())
            {
                ConfigureWindowsProcess(process);
            }
            else if (ConfigureMacProcess != null && OperatingSystem.IsMacOS())
            {
                ConfigureMacProcess(process);
            }
            else if (ConfigureLinuxProcess != null && OperatingSystem.IsLinux())
            {
                ConfigureLinuxProcess(process);
            }
            
            Log?.LogInformation($"Starting Node.js dev server in: {absoluteWorkingDir}");
            if (!process.Start())
            {
                Log?.LogError("Failed to start Node.js process");
                return false;
            }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            Log?.LogInformation("Node.js dev server started successfully");
            return true;
        }
        catch (Exception ex)
        {
            Log?.LogError(ex, "Error starting Node.js process");
            return false;
        }
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
                ClearCache();
            }
            else
            {
                RemoveCacheEntry(cacheKey);
            }
        }

        var shouldCache = ShouldCache(context);
        if (shouldCache && _cache.TryGetValue(cacheKey, out var cached))
        {
            Interlocked.Increment(ref _cacheHits);
            cached.LastAccessTime = DateTime.UtcNow;
            if (LogDebug) Log?.LogDebug("Cache hit: {CacheKey} |mimeType| {MimeType} |encoding| {Encoding} |size| {Size}",
                cacheKey, cached.MimeType, cached.Encoding, cached.Size);
            context.Response.ContentType = cached.MimeType;
            if (!string.IsNullOrEmpty(cached.Encoding))
            {
                context.Response.Headers["Content-Encoding"] = cached.Encoding;
            }
            await context.Response.Body.WriteAsync(cached.Data, context.RequestAborted);
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
                    Interlocked.Increment(ref _cacheMisses);

                    // Check if file size exceeds individual file limit
                    if (bytes.Length > MaxFileSizeBytes)
                    {
                        Log?.LogInformation("Cache skip (too large): {CacheKey} |size| {Length} |limit| {MaxFileSizeBytes}",
                            cacheKey, bytes.Length, MaxFileSizeBytes);
                        await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
                        return;
                    }

                    var mimeType = response.Content.Headers.ContentType?.ToString()
                        ?? ServiceStack.MimeTypes.GetMimeType(cacheKey);
                    var encoding = response.Content.Headers.ContentEncoding.FirstOrDefault();

                    var entry = new CacheEntry(mimeType, bytes, encoding, DateTime.UtcNow);

                    // Add to cache with size management
                    AddToCache(cacheKey, entry);

                    Log?.LogInformation("Cache miss: {CacheKey} |mimeType| {MimeType} |encoding| {Encoding} |size| {Bytes}",
                        cacheKey, mimeType, encoding, bytes.Length);
                    await context.Response.Body.WriteAsync(bytes, context.RequestAborted);
                    return;
                }
            }
        }

        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }

    public string ConnectingHtml { get; set; } = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1.0">
            <title>Connecting to Development Server</title>
            <style>
                * {
                    margin: 0;
                    padding: 0;
                    box-sizing: border-box;
                }
                body {
                    font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                    min-height: 100vh;
                    display: flex;
                    justify-content: center;
                    align-items: center;
                    background: #0f0f1a;
                    overflow: hidden;
                }
                .background {
                    position: fixed;
                    inset: 0;
                    z-index: 0;
                }
                .background::before {
                    content: '';
                    position: absolute;
                    top: -50%;
                    left: -50%;
                    width: 200%;
                    height: 200%;
                    background: 
                        radial-gradient(circle at 20% 80%, rgba(6, 182, 212, 0.15) 0%, transparent 50%),
                        radial-gradient(circle at 80% 20%, rgba(20, 184, 166, 0.15) 0%, transparent 50%),
                        radial-gradient(circle at 40% 40%, rgba(34, 211, 238, 0.1) 0%, transparent 40%);
                    animation: float 20s ease-in-out infinite;
                }
                @keyframes float {
                    0%, 100% { transform: translate(0, 0) rotate(0deg); }
                    33% { transform: translate(30px, -30px) rotate(5deg); }
                    66% { transform: translate(-20px, 20px) rotate(-5deg); }
                }
                .container {
                    position: relative;
                    z-index: 1;
                    text-align: center;
                    padding: 3rem 4rem;
                    background: rgba(255, 255, 255, 0.03);
                    border: 1px solid rgba(255, 255, 255, 0.08);
                    border-radius: 24px;
                    backdrop-filter: blur(20px);
                    box-shadow: 
                        0 25px 50px -12px rgba(0, 0, 0, 0.5),
                        inset 0 1px 0 rgba(255, 255, 255, 0.1);
                }
                .icon-wrapper {
                    width: 80px;
                    height: 80px;
                    margin: 0 auto 2rem;
                    position: relative;
                }
                .pulse-ring {
                    position: absolute;
                    inset: 0;
                    border-radius: 50%;
                    border: 2px solid rgba(6, 182, 212, 0.3);
                    animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
                }
                .pulse-ring:nth-child(2) { animation-delay: 0.5s; }
                .pulse-ring:nth-child(3) { animation-delay: 1s; }
                @keyframes pulse {
                    0% { transform: scale(1); opacity: 1; }
                    100% { transform: scale(2); opacity: 0; }
                }
                .icon-circle {
                    position: absolute;
                    inset: 0;
                    background: linear-gradient(135deg, #0891b2 0%, #06b6d4 50%, #22d3ee 100%);
                    border-radius: 50%;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    box-shadow: 0 10px 40px rgba(6, 182, 212, 0.4);
                }
                .icon-circle svg {
                    width: 36px;
                    height: 36px;
                    color: white;
                    animation: rotate 3s linear infinite;
                }
                @keyframes rotate {
                    from { transform: rotate(0deg); }
                    to { transform: rotate(360deg); }
                }
                h1 {
                    font-size: 1.75rem;
                    font-weight: 600;
                    color: #ffffff;
                    margin-bottom: 0.75rem;
                    letter-spacing: -0.025em;
                }
                .subtitle {
                    font-size: 1rem;
                    color: rgba(255, 255, 255, 0.5);
                    margin-bottom: 2.5rem;
                    font-weight: 400;
                }
                .progress-container {
                    width: 100%;
                    max-width: 280px;
                    margin: 0 auto;
                }
                .progress-bar {
                    height: 4px;
                    background: rgba(255, 255, 255, 0.1);
                    border-radius: 100px;
                    overflow: hidden;
                }
                .progress-fill {
                    height: 100%;
                    width: 30%;
                    background: linear-gradient(90deg, #0891b2, #22d3ee, #0891b2);
                    background-size: 200% 100%;
                    border-radius: 100px;
                    animation: shimmer 1.5s ease-in-out infinite, progress 2s ease-in-out infinite;
                }
                @keyframes shimmer {
                    0% { background-position: 200% 0; }
                    100% { background-position: -200% 0; }
                }
                @keyframes progress {
                    0% { width: 20%; margin-left: 0; }
                    50% { width: 40%; margin-left: 30%; }
                    100% { width: 20%; margin-left: 80%; }
                }
                .status {
                    margin-top: 1.5rem;
                    font-size: 0.875rem;
                    color: rgba(255, 255, 255, 0.4);
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    gap: 0.5rem;
                }
                .dot {
                    width: 6px;
                    height: 6px;
                    background: #22c55e;
                    border-radius: 50%;
                    animation: blink 1.5s ease-in-out infinite;
                }
                @keyframes blink {
                    0%, 100% { opacity: 1; }
                    50% { opacity: 0.3; }
                }
                .decorative-dots {
                    position: absolute;
                    width: 6px;
                    height: 6px;
                    background: rgba(255, 255, 255, 0.1);
                    border-radius: 50%;
                }
                .decorative-dots:nth-child(1) { top: 20px; left: 20px; }
                .decorative-dots:nth-child(2) { top: 20px; right: 20px; }
                .decorative-dots:nth-child(3) { bottom: 20px; left: 20px; }
                .decorative-dots:nth-child(4) { bottom: 20px; right: 20px; }
            </style>
        </head>
        <body>
            <div class="background"></div>
            <div class="container">
                <div class="decorative-dots"></div>
                <div class="decorative-dots"></div>
                <div class="decorative-dots"></div>
                <div class="decorative-dots"></div>
                <div class="icon-wrapper">
                    <div class="pulse-ring"></div>
                    <div class="pulse-ring"></div>
                    <div class="pulse-ring"></div>
                    <div class="icon-circle">
                        <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                            <path stroke-linecap="round" stroke-linejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                        </svg>
                    </div>
                </div>
                <h1>Starting Development Server</h1>
                <p class="subtitle">Preparing your development environment</p>
                <div class="progress-container">
                    <div class="progress-bar">
                        <div class="progress-fill"></div>
                    </div>
                </div>
                <div class="status">
                    <span class="dot"></span>
                    <span>Connecting to Node.js server...</span>
                </div>
            </div>
            <script>
                async function checkServer() {
                    try {
                        const response = await fetch('/', {
                            method: 'HEAD',
                            cache: 'no-cache'
                        });
                        if (response.ok) {
                            window.location.reload();
                        } else {
                            setTimeout(checkServer, 200);
                        }
                    } catch (e) {
                        // Server still not ready, try again
                        setTimeout(checkServer, 200);
                    }
                }
                // Start polling
                checkServer();
            </script>
        </body>
        </html>
        """;
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
                if (fileInfo is { Exists: true, IsDirectory: false })
                {
                    context.Response.ContentType = "text/html";
                    await using var stream = fileInfo.CreateReadStream();
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
    /// Map Vite HMR WebSocket requests
    /// </summary>
    public static void MapViteHmr(this WebApplication app, NodeProxy proxy)
    {
        app.Use(async (context, next) =>
        {
            // Vite HMR uses WebSocket connections on the root path
            // Check if this is a WebSocket upgrade request
            if (context.WebSockets.IsWebSocketRequest)
            {
                await WebSocketToNode(context, proxy.Client.BaseAddress!);
            }
            else
            {
                await next();
            }
        });
    }

    /// <summary>
    /// Proxy WebSocket requests to Node.js
    /// </summary>
    public static async Task WebSocketToNode(HttpContext context, Uri nextServerBase, bool allowInvalidCerts=true)
    {
        // Handle WebSocket subprotocol if requested (Vite HMR uses this)
        string? requestedProtocol = null;
        if (context.Request.Headers.TryGetValue("Sec-WebSocket-Protocol", out var protocolValues))
        {
            requestedProtocol = protocolValues.ToString();
        }

        var acceptOptions = string.IsNullOrEmpty(requestedProtocol)
            ? null
            : new WebSocketAcceptContext { SubProtocol = requestedProtocol };

        using var clientSocket = acceptOptions != null
            ? await context.WebSockets.AcceptWebSocketAsync(acceptOptions)
            : await context.WebSockets.AcceptWebSocketAsync();

        using var nextSocket = new ClientWebSocket();
        if (allowInvalidCerts && nextServerBase.Scheme == "https")
        {
            nextSocket.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        }

        if (context.Request.Headers.TryGetValue("Cookie", out var cookieValues))
        {
            nextSocket.Options.SetRequestHeader("Cookie", cookieValues.ToString());
        }

        // Add WebSocket subprotocol if requested
        if (!string.IsNullOrEmpty(requestedProtocol))
        {
            nextSocket.Options.AddSubProtocol(requestedProtocol);
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
    /// Run Next.js dev server if not already running by checking for lock file
    /// </summary>
    public static System.Diagnostics.Process? RunNodeProcess(this WebApplication app,
        NodeProxy proxy,
        string lockFile,
        string workingDirectory, 
        bool registerExitHandler=true)
    {
        var process = app.StartNodeProcess(proxy, lockFile, workingDirectory, registerExitHandler);
        if (process != null)
        {
            proxy.Log?.LogInformation("Started Next.js dev server");
        }
        else
        {
            proxy.Log?.LogInformation("Next.js dev server already running");
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

            process.Exited += (s, e) => {
                proxy.Log?.LogDebug("Exited: " + process.ExitCode);
                File.Delete(lockFile);
            };

            if (registerExitHandler)
            {
                app.Lifetime.ApplicationStopping.Register(() => {
                    if (!process.HasExited)
                    {
                        proxy.Log?.LogDebug("Terminating process: " + process.Id);
                        process.Kill(entireProcessTree: true);
                    }
                });
            }
            return process;
        }

        return null;
    }


    /// <summary>
    /// Run Next.js dev server if not already running by checking for port availability
    /// </summary>
    public static System.Diagnostics.Process? RunNodeProcess(this WebApplication app,
        NodeProxy proxy,
        string workingDirectory, 
        bool registerExitHandler=true)
    {
        var process = app.StartNodeProcess(proxy, workingDirectory);
        if (process != null)
        {
            proxy.Log?.LogInformation("Started Next.js dev server");
        }
        else
        {
            proxy.Log?.LogInformation("Next.js dev server already running");
        }
        return process;
    }

    public static System.Diagnostics.Process? StartNodeProcess(this WebApplication app, 
        NodeProxy proxy,
        string workingDirectory)
    {
        if (proxy.IsPortAvailable())
        {
            if (!proxy.TryStartNode(workingDirectory, out var process))
                return null;

            process.Exited += (s, e) => {
                proxy.Log?.LogDebug("Exited: " + process.ExitCode);
            };

            app.Lifetime.ApplicationStopping.Register(() => {
                if (!process.HasExited)
                {
                    proxy.Log?.LogDebug("Terminating process: " + process.Id);
                    process.Kill(entireProcessTree: true);
                }
            });
            return process;
        }
        return null;
    }
    
    public static IEndpointConventionBuilder MapFallbackToNode(this WebApplication app, NodeProxy proxy)
    {
        return app.MapFallback(async (HttpContext context) =>
        {
            try
            {
                await proxy.HttpToNode(context);
            }
            catch (SocketException)
            {
                context.Response.StatusCode = 503;
                context.Response.ContentType = "text/html";
                await context.Response.WriteAsync(proxy.ConnectingHtml);
            }
        });
    }
}
