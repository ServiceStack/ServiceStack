using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class AuthModernizationTests
{
    class MockExtendedSession : AuthUserSession, IAuthSessionExtended
    {
        public bool OnLogoutAsyncCalled { get; set; }
        public bool OnAuthenticatedAsyncCalled { get; set; }
        public bool OnRegisteredAsyncCalled { get; set; }

        public new Task OnLogoutAsync(IServiceBase authService, CancellationToken token = default)
        {
            OnLogoutAsyncCalled = true;
            return Task.CompletedTask;
        }

        public new Task OnAuthenticatedAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token = default)
        {
            OnAuthenticatedAsyncCalled = true;
            return Task.CompletedTask;
        }

        public new Task OnRegisteredAsync(IRequest httpReq, IAuthSession session, IServiceBase authService, CancellationToken token = default)
        {
            OnRegisteredAsyncCalled = true;
            return Task.CompletedTask;
        }

        public new Task<IHttpResult> ValidateAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo, CancellationToken token = default)
        {
            return Task.FromResult<IHttpResult>(null);
        }
    }

    class MockDisposableAuthRepo : InMemoryAuthRepository, IDisposable, IAsyncDisposable
    {
        public bool Disposed { get; private set; }
        public bool DisposedAsync { get; private set; }

        public void Dispose()
        {
            Disposed = true;
        }

        public ValueTask DisposeAsync()
        {
            DisposedAsync = true;
            return default;
        }
    }

    class TestAuthProvider : AuthProvider
    {
        public TestAuthProvider()
        {
            Provider = "test";
        }

        public override bool IsAuthorized(IAuthSession session, IAuthTokens tokens, Authenticate request = null) => true;

        public override Task<object> AuthenticateAsync(IServiceBase authService, IAuthSession session, Authenticate request, CancellationToken token = default) =>
            Task.FromResult<object>(new AuthenticateResponse());
    }

    [Test]
    public async Task LogoutAsync_Invokes_OnLogoutAsync_On_IAuthSessionExtended()
    {
        using var appHost = new BasicAppHost().Init();
        var provider = new TestAuthProvider();
        var session = new MockExtendedSession();
        var req = new MockHttpRequest();
        req.Items[Keywords.Session] = session;
        var authService = new AuthenticateService { Request = req };

        await provider.LogoutAsync(authService, new Authenticate(), CancellationToken.None);

        Assert.That(session.OnLogoutAsyncCalled, Is.True, "LogoutAsync should invoke session.OnLogoutAsync on IAuthSessionExtended");
    }

    [Test]
    public void UserAuthRepositoryAsyncWrapper_Disposes_Underlying_AuthRepo()
    {
        var mockRepo = new MockDisposableAuthRepo();
        var wrapper = new UserAuthRepositoryAsyncWrapper(mockRepo);

        wrapper.Dispose();
        Assert.That(mockRepo.Disposed, Is.True, "Synchronous Dispose should forward to inner repository");
    }

    [Test]
    public async Task UserAuthRepositoryAsyncWrapper_DisposesAsync_Underlying_AuthRepo()
    {
        var mockRepo = new MockDisposableAuthRepo();
        var wrapper = new UserAuthRepositoryAsyncWrapper(mockRepo);

        await wrapper.DisposeAsync();
        Assert.That(mockRepo.DisposedAsync, Is.True, "Asynchronous DisposeAsync should forward to inner repository");
    }

    [Test]
    public void PasswordHasher_VerifyPassword_Handles_Invalid_Base64_Gracefully()
    {
        var hasher = new PasswordHasher();
        // Invalid base64 characters
        var result = hasher.VerifyPassword("not-a-valid-base64!@#$%", "password", out var needsRehash);
        Assert.That(result, Is.False);
    }

    [Test]
    public void SaltedHash_VerifyHash_Handles_Malformed_Hashes_Gracefully()
    {
        var saltedHash = new SaltedHash();
        var result = saltedHash.VerifyHashString("invalid-hash-format", "password", "salt");
        Assert.That(result, Is.False);
    }

    [Test]
    public void SaltedHash_FixedTime_Comparison_Validates_Correctly()
    {
        var saltedHash = new SaltedHash();
        saltedHash.GetHashAndSaltString("TestSecret123!", out var hash, out var salt);
        Assert.That(saltedHash.VerifyHashString("TestSecret123!", hash, salt), Is.True);
        Assert.That(saltedHash.VerifyHashString("WrongPassword", hash, salt), Is.False);
    }

    [Test]
    public void ApiKeyAuthProvider_CreateApiKey_Handles_Different_Byte_Sizes()
    {
        var provider = new ApiKeyAuthProvider();
        var key16 = provider.CreateApiKey("live", "secret", 16);
        var key32 = provider.CreateApiKey("live", "secret", 32);
        var key64 = provider.CreateApiKey("live", "secret", 64);

        Assert.That(key16, Is.Not.Null.And.Not.Empty);
        Assert.That(key32, Is.Not.Null.And.Not.Empty);
        Assert.That(key64, Is.Not.Null.And.Not.Empty);
        Assert.That(key16.Length, Is.LessThan(key32.Length));
        Assert.That(key32.Length, Is.LessThan(key64.Length));
    }

    [Test]
    public void SocialExtensions_ToGravatarUrl_Handles_Null_And_Whitespace()
    {
        var urlNull = ((string)null).ToGravatarUrl();
        Assert.That(urlNull, Does.StartWith("https://www.gravatar.com/avatar/"));

        var urlWhitespace = "   ".ToGravatarUrl();
        Assert.That(urlWhitespace, Does.StartWith("https://www.gravatar.com/avatar/"));

        var urlNormal = "Test.User@Example.com ".ToGravatarUrl();
        var urlClean = "test.user@example.com".ToGravatarUrl();
        Assert.That(urlNormal, Is.EqualTo(urlClean), "Gravatar URL should trim and lower-case emails");
    }

    [Test]
    public void UserAuth_ConvertSessionToClaims_Does_Not_Duplicate_Phone_Claims()
    {
        var session = new AuthUserSession
        {
            UserAuthId = "1",
            UserName = "testuser",
            Email = "test@example.com",
            HomePhone = "123-456",
            MobilePhone = "789-012"
        };

        var claims = session.ConvertSessionToClaims();
        var homePhones = claims.Where(c => c.Type == ClaimTypes.HomePhone).ToList();
        var mobilePhones = claims.Where(c => c.Type == ClaimTypes.MobilePhone).ToList();

        Assert.That(homePhones.Count, Is.EqualTo(1));
        Assert.That(mobilePhones.Count, Is.EqualTo(1));
    }

    [Test]
    public void InMemoryAuthRepository_Creates_And_Retrieves_User()
    {
        var repo = new InMemoryAuthRepository();
        repo.CreateUserAuth(new UserAuth { UserName = "user1", Email = "u1@test.com" }, "pass1");

        var userAuth = repo.GetUserAuthByUserName("user1");
        Assert.That(userAuth, Is.Not.Null);
        Assert.That(userAuth.Email, Is.EqualTo("u1@test.com"));
    }

    [Test]
    public void DigestAuthFunctions_Handles_Missing_Keys_Gracefully()
    {
        var helper = new DigestAuthFunctions();
        var incompleteHeaders = new Dictionary<string, string>();

        var isValid = helper.ValidateResponse(incompleteHeaders, "key", 600, "ha1", "seq");
        Assert.That(isValid, Is.False);
    }
}
