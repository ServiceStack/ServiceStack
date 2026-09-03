#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.IO;
using ServiceStack.Web;

namespace ServiceStack.Extensions.Tests;

[TestFixture]
public class SecurityRemediationTests
{
    private class DummyRequest : IRequest
    {
        public object OriginalRequest => this;
        public IResponse Response => null!;
        public string OperationName { get; set; } = "";
        public string Verb { get; set; } = "GET";
        public RequestAttributes RequestAttributes { get; set; }
        public IRequestPreferences RequestPreferences => null!;
        public string ContentType { get; set; } = "";
        public bool IsLocal => true;
        public string ResponseContentType { get; set; } = "";
        public bool HasExplicitResponseContentType { get; set; }
        public IDictionary<string, Cookie> Cookies => new Dictionary<string, Cookie>();
        public Dictionary<string, object> Items => new();
        public NameValueCollection Headers => new();
        public NameValueCollection QueryString => new();
        public NameValueCollection FormData => new();
        public Stream InputStream => Stream.Null;
        public string RawUrl => "";
        public string AbsoluteUri => "";
        public string UserHostAddress => "";
        public string RemoteIp => "";
        public bool IsSecureConnection => false;
        public string[] AcceptTypes => [];
        public string PathInfo => "";
        public IHttpFile[] Files => [];
        public object? Dto { get; set; }
        public IResolver Resolver => null!;
        public T TryResolve<T>() => default!;
        public object GetService(Type serviceType) => null!;
        public string? GetHeader(string headerName) => null;
        public string? Authorization => null;
        public Uri? UrlReferrer => null;
        public IVirtualFile? GetFile() => null;
        public IVirtualDirectory? GetDirectory() => null;
        public bool IsFile => false;
        public bool IsDirectory => false;
        public string? UserAgent => null;
        public string? CompressionType => null;
        public IHttpResponse? HttpResponse => null;
        public string? HttpMethod => "GET";
        public string? XForwardedFor => null;
        public int? XForwardedPort => null;
        public string? XForwardedProtocol => null;
        public string? XRealIp => null;
        public string? Accept => null;
        public string? GetRawBody() => null;
        public Task<string?> GetRawBodyAsync() => Task.FromResult((string?)null);
        public bool UseBufferedStream { get; set; }
        public string OriginalPathInfo => "";
        public long ContentLength => 0;
        public System.Threading.CancellationToken RequestAborted => System.Threading.CancellationToken.None;
    }

    [Test]
    public void IdentityException_WithEmptyErrors_DoesNotThrowArgumentOutOfRangeException()
    {
        var ex = new IdentityException([]);
        Assert.That(ex.Message, Is.EqualTo("Identity operation failed"));
        Assert.That(ex.Code, Is.Null);
        Assert.That(ex.StatusCode, Is.EqualTo(400));
        
        var status = ex.ToResponseStatus();
        Assert.That(status.ErrorCode, Is.EqualTo("IdentityException"));
        Assert.That(status.Errors, Is.Empty);
    }

    [Test]
    public void IdentityException_WithErrors_PopulatesFirstErrorDescriptionAndCode()
    {
        var ex = new IdentityException([
            new IdentityError { Code = "PasswordTooShort", Description = "Passwords must be at least 6 characters." },
            new IdentityError { Code = "PasswordRequiresDigit", Description = "Passwords must have at least one digit ('0'-'9')." },
        ]);
        Assert.That(ex.Message, Is.EqualTo("Passwords must be at least 6 characters."));
        Assert.That(ex.Code, Is.EqualTo("PasswordTooShort"));
        
        var status = ex.ToResponseStatus();
        Assert.That(status.Errors, Is.Not.Null);
        Assert.That(status.Errors!.Count, Is.EqualTo(2));
        Assert.That(status.Errors[0].ErrorCode, Is.EqualTo("PasswordTooShort"));
        Assert.That(status.Errors[1].ErrorCode, Is.EqualTo("PasswordRequiresDigit"));
    }

    [Test]
    public void GetClaimsPrincipalRoles_WithoutIdentityOptions_FallsBackToStandardRoleClaim()
    {
        var mockReq = new DummyRequest();
        var identity = new ClaimsIdentity([
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Manager"),
            new Claim(ClaimTypes.Name, "testuser"),
        ]);
        var principal = new ClaimsPrincipal(identity);

        var roles = mockReq.GetClaimsPrincipalRoles(principal);
        Assert.That(roles, Is.EquivalentTo(new[] { "Admin", "Manager" }));
    }

    [Test]
    public void NodeProxy_DefaultShouldCache_HandlesNullOrLocalhostGracefully()
    {
        using var client = new HttpClient();
        var proxy = new NodeProxy(client);

        // Localhost should not cache
        var localContext = new DefaultHttpContext();
        localContext.Request.Host = new HostString("localhost", 5000);
        Assert.That(proxy.DefaultShouldCache(localContext), Is.False);

        // Null/empty host should not throw NRE
        var nullHostContext = new DefaultHttpContext();
        Assert.That(proxy.DefaultShouldCache(nullHostContext), Is.False);
    }

    private class InterceptingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? CapturedRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    [Test]
    public async Task NodeProxy_HttpToNode_PreservesContentTypeOnRequestBody()
    {
        var handler = new InterceptingHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:3000") };
        var proxy = new NodeProxy(client);

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/items";
        context.Request.ContentType = "application/json";
        context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{\"name\":\"test\"}"));

        await proxy.HttpToNode(context);

        Assert.That(handler.CapturedRequest, Is.Not.Null);
        Assert.That(handler.CapturedRequest!.Content, Is.Not.Null);
        Assert.That(handler.CapturedRequest.Content!.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
    }

    private class TestOptionsMonitor<T>(T currentValue) : IOptionsMonitor<T>
    {
        public T CurrentValue { get; } = currentValue;
        public T Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }

    [Test]
    public async Task BasicAuthenticationHandler_HandlesMalformedBase64_WithoutThrowingFormatException()
    {
        var optionsMonitor = new TestOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions());
        var scheme = new AuthenticationScheme("Basic", "Basic", typeof(BasicAuthenticationHandler<IdentityUser>));
        var handler = new BasicAuthenticationHandler<IdentityUser>(
            signInManager: null!,
            options: optionsMonitor,
            logger: NullLoggerFactory.Instance,
            encoder: UrlEncoder.Default);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = "Basic !!!invalid_base64!!!";

        await handler.InitializeAsync(scheme, httpContext);
        var result = await handler.AuthenticateAsync();

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.Failure, Is.Not.Null);
        Assert.That(result.Failure!.Message, Does.Contain("Invalid Base64"));
    }

    [Test]
    public async Task BasicAuthenticationHandler_HandlesBearerOrEmpty_ReturnsNoResult()
    {
        var optionsMonitor = new TestOptionsMonitor<AuthenticationSchemeOptions>(new AuthenticationSchemeOptions());
        var scheme = new AuthenticationScheme("Basic", "Basic", typeof(BasicAuthenticationHandler<IdentityUser>));
        var handler = new BasicAuthenticationHandler<IdentityUser>(
            signInManager: null!,
            options: optionsMonitor,
            logger: NullLoggerFactory.Instance,
            encoder: UrlEncoder.Default);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = "Bearer some.jwt.token";

        await handler.InitializeAsync(scheme, httpContext);
        var result = await handler.AuthenticateAsync();

        Assert.That(result.None, Is.True);
    }
}
