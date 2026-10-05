using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;

namespace ServiceStack.AI;

/// <summary>Local-only OAuth receiver. Starts before authorization, never logs query strings,
/// and delegates credential ownership and verification to the existing pending-flow machinery.</summary>
internal sealed class OpenAiSubscriptionCallback(OpenAiSubscriptionFlow flows, Action<string> connected)
{
    readonly SemaphoreSlim gate = new(1, 1);
    WebApplication? listener;
    Uri? redirect;
    bool closed;

    public async Task<string> StartAsync(string configuredUri, CancellationToken token)
    {
        var uri = new Uri(configuredUri);
        if (uri.Scheme != "http" || uri.Host != "127.0.0.1" || uri.AbsolutePath != "/auth/callback" || uri.Query.Length != 0 || uri.Fragment.Length != 0 || uri.UserInfo.Length != 0)
            throw HttpError.BadRequest("Automatic local sign-in requires http://127.0.0.1:<port>/auth/callback.");
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (closed) throw new ObjectDisposedException(nameof(OpenAiSubscriptionCallback));
            if (listener != null) return redirect!.AbsoluteUri;
            try { await ListenAsync(uri.Port, token).ConfigureAwait(false); }
            catch (IOException e) when (AddressInUse(e)) { await ListenAsync(0, token).ConfigureAwait(false); }
            return redirect!.AbsoluteUri;
        }
        catch (IOException)
        { throw new HttpError(503, "OpenAiCallbackUnavailable", "Could not start the local OpenAI callback listener. Check this application's permission to listen on 127.0.0.1."); }
        finally { gate.Release(); }
    }
    static bool AddressInUse(Exception error)
    {
        for (Exception? e = error; e != null; e = e.InnerException)
            if (e is SocketException { SocketErrorCode: SocketError.AddressAlreadyInUse } || e is Microsoft.AspNetCore.Connections.AddressInUseException) return true;
        return false;
    }
    async Task ListenAsync(int port, CancellationToken token)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = [], ApplicationName = typeof(OpenAiSubscriptionCallback).Assembly.FullName });
        builder.Configuration.Sources.Clear();
        builder.Logging.ClearProviders(); // OAuth URLs must never enter request/startup logs.
        builder.WebHost.ConfigureKestrel(server => {
            server.Listen(IPAddress.Loopback, port);
            server.Limits.MaxRequestLineSize = 16384;
            server.Limits.MaxRequestBodySize = 0;
            server.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(10);
        });
        var app = builder.Build();
        app.Run(async (HttpContext request) => {
            request.Response.Headers.CacheControl = "no-store";
            request.Response.Headers["Referrer-Policy"] = "no-referrer";
            request.Response.Headers["X-Content-Type-Options"] = "nosniff";
            if (request.Request.Method != "GET" || request.Request.Path != "/auth/callback" || redirect == null || request.Request.Host.Value != redirect.Authority)
            { request.Response.StatusCode = 404; return; }
            try
            {
                var result = await flows.AutomaticCallbackAsync(request.Request.GetEncodedUrl(), request.RequestAborted).ConfigureAwait(false);
                connected(result.User);
                request.Response.ContentType = "text/html; charset=utf-8";
                await request.Response.WriteAsync(Page(result.ReturnUrl), request.RequestAborted).ConfigureAwait(false);
            }
            catch (HttpError e)
            {
                request.Response.StatusCode = (int)e.StatusCode;
                request.Response.ContentType = "text/html; charset=utf-8";
                await request.Response.WriteAsync(Page(null, e.Message), request.RequestAborted).ConfigureAwait(false);
            }
            catch (Exception) when (!request.RequestAborted.IsCancellationRequested)
            {
                request.Response.StatusCode = 500;
                request.Response.ContentType = "text/html; charset=utf-8";
                await request.Response.WriteAsync(Page(null, "OpenAI sign-in could not be completed. Return to the app and start a new sign-in."), request.RequestAborted).ConfigureAwait(false);
            }
        });
        try
        {
            await app.StartAsync(token).ConfigureAwait(false);
            redirect = new Uri(app.Urls.Single().TrimEnd('/') + "/auth/callback");
            listener = app;
        }
        catch { await app.DisposeAsync().ConfigureAwait(false); throw; }
    }
    internal static string Page(string? returnUrl, string? error = null)
    {
        var title = error == null ? "Connected to ChatGPT" : "ChatGPT sign-in failed";
        var message = error ?? "Sign-in is complete. You can return to the app.";
        var link = returnUrl == null ? "" : $"<p><a href=\"{WebUtility.HtmlEncode(returnUrl)}\">Return to app</a></p>";
        var script = error != null ? "" : "<script>history.replaceState(null,'',location.pathname);window.close();" +
            (returnUrl == null ? "" : "setTimeout(()=>location.replace(" + JsonSerializer.Serialize(returnUrl) + "),100);") + "</script>";
        return $"<!doctype html><html><head><meta charset=\"utf-8\"><meta name=\"referrer\" content=\"no-referrer\"><title>{title}</title></head><body><h1>{title}</h1><p>{WebUtility.HtmlEncode(message)}</p>{link}{script}</body></html>";
    }
    public async Task CloseAsync(CancellationToken token)
    {
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            closed = true;
            if (listener != null) { await listener.StopAsync(token).ConfigureAwait(false); await listener.DisposeAsync().ConfigureAwait(false); listener = null; }
        }
        finally { gate.Release(); }
    }
}
