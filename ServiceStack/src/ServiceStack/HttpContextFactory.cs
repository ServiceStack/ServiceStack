#if NET6_0_OR_GREATER
#nullable enable

using System.Security.Claims;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using ServiceStack;

namespace ServiceStack;

public class HttpContextFactory
{
    public static HttpContext CreateHttpContext(string baseUrl)
    {
        var testRequest = new TestRequest(baseUrl);
        var testResponse = new TestResponse();
        var httpCtx = new TestHttpContext(testRequest, testResponse) { };
        testRequest.httpContext = httpCtx;
        testResponse.httpContext = httpCtx;
        return httpCtx;
    }

    public class TestRequest : HttpRequest
    {
        public TestRequest(string baseUrl)
        {
            Scheme = baseUrl.LeftPart("://");
            IsHttps = Scheme == "https";
        }
        public HttpContext httpContext;
        public override HttpContext HttpContext => httpContext;
        public override string Method { get; set; } = "GET";
        public override string Scheme { get; set; }
        public override bool IsHttps { get; set; }
        public override HostString Host { get; set; } = new HostString("localhost");
        public override PathString PathBase { get; set; } = new("/");
        public override PathString Path { get; set; } = new("/");
        public override QueryString QueryString { get; set; } = new();
        public override IQueryCollection Query { get; set; } = new QueryCollection();
        public override string Protocol { get; set; } = "HTTP 1.1";
        public override IHeaderDictionary Headers { get; } = new HeaderDictionary();
        public override IRequestCookieCollection Cookies { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override long? ContentLength { get; set; }
        public override string? ContentType { get; set; }
        public override Stream Body { get; set; } = new MemoryStream();
        public override bool HasFormContentType { get; } = false;
        public override IFormCollection Form { get; set; } = new FormCollection(new());
        public override Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken = default) => Task.FromResult(Form);
    }

    public class TestResponse : HttpResponse
    {
        public HttpContext httpContext;
        public override HttpContext HttpContext => httpContext;

        public override int StatusCode { get; set; } = 200;
        public override IHeaderDictionary Headers { get; } = new HeaderDictionary();
        public override Stream Body { get; set; } = new MemoryStream();
        public override long? ContentLength { get; set; }
        public override string ContentType { get; set; } = MimeTypes.Html;
        public override IResponseCookies Cookies => throw new NotImplementedException();
        public override bool HasStarted { get; } = false;
        public override void OnCompleted(Func<object, Task> callback, object state) { }
        public override void OnStarting(Func<object, Task> callback, object state) { }
        public override void Redirect(string location, bool permanent) { }
    }

    public class TestHttpContext : HttpContext
    {
        public TestHttpContext(HttpRequest request, HttpResponse response)
        {
            Request = request;
            Response = response;
        }

        public override HttpRequest Request { get; }
        public override HttpResponse Response { get; }
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override Microsoft.AspNetCore.Http.ConnectionInfo Connection { get; } = new TestConnectionInfo();
        public override WebSocketManager WebSockets => throw new NotImplementedException();

        public override ClaimsPrincipal User { get; set; } = new();
        public override IDictionary<object, object?> Items { get; set; } = new Dictionary<object, object?>();
        public override IServiceProvider RequestServices { get; set; } = HostContext.AppHost.GetContainer();
        public override CancellationToken RequestAborted { get; set; } = new();
        public override string TraceIdentifier { get; set; } = Guid.NewGuid().ToString();
        public override ISession Session { get; set; } = new TestSession();
        public override void Abort() { }
    }

    class TestConnectionInfo : Microsoft.AspNetCore.Http.ConnectionInfo
    {
        public override string Id { get; set; } = Guid.NewGuid().ToString();
        public override IPAddress? RemoteIpAddress { get; set; } = IPAddress.Loopback;
        public override int RemotePort { get; set; } = 80;
        public override IPAddress? LocalIpAddress { get; set; } = IPAddress.Loopback;
        public override int LocalPort { get; set; } = 80;
        public override X509Certificate2? ClientCertificate { get; set; }
        public override Task<X509Certificate2?> GetClientCertificateAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(null as X509Certificate2);
        }
    }

    class TestSession : ISession
    {
        public bool IsAvailable => false;
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public IEnumerable<string> Keys { get; set; } = Array.Empty<string>();
        public void Clear() { }
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Remove(string key) { }
        public void Set(string key, byte[] value) { }
        public bool TryGetValue(string key, [NotNullWhen(true)] out byte[]? value)
        {
            value = null;
            return false;
        }
    }
}

#endif