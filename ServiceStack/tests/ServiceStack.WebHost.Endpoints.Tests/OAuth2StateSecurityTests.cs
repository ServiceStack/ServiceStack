using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.OrmLite;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class OAuth2StateSecurityTests
{
    private class StubOAuth2Provider : OAuth2Provider
    {
        public int AccessTokenExchangeCalls { get; private set; }
        public string StubUserId { get; set; } = "oauth-user-1";
        public string StubEmail { get; set; } = "attacker@example.test";

        public StubOAuth2Provider(IAppSettings appSettings)
            : base(appSettings, "https://idp.example.test/", "stuboauth2")
        {
            ConsumerKey = "client-id";
            ConsumerSecret = "client-secret";
            AuthorizeUrl = "https://idp.example.test/authorize";
            AccessTokenUrl = "https://idp.example.test/token";
            CallbackUrl = "https://app.example.test/auth/stuboauth2";
            RedirectUrl = "https://app.example.test/";
        }

        protected override Task<string> GetAccessTokenJsonAsync(string code, AuthContext ctx, CancellationToken token = default)
        {
            AccessTokenExchangeCalls++;
            return Task.FromResult("{\"access_token\":\"stub-access-token\"}");
        }

        protected override Task<Dictionary<string, string>> CreateAuthInfoAsync(string accessToken, CancellationToken token = default)
        {
            return Task.FromResult(new Dictionary<string, string>
            {
                ["user_id"] = StubUserId,
                ["username"] = "stubuser",
                ["name"] = "Stub User",
                ["email"] = StubEmail,
            });
        }
    }

    private class StubOAuth2ProviderSync : OAuth2ProviderSync
    {
        public int AccessTokenExchangeCalls { get; private set; }
        public string StubUserId { get; set; } = "oauth-sync-user-1";
        public string StubEmail { get; set; } = "attacker-sync@example.test";

        public StubOAuth2ProviderSync(IAppSettings appSettings)
            : base(appSettings, "https://idp.example.test/", "stuboauth2sync")
        {
            ConsumerKey = "client-id";
            ConsumerSecret = "client-secret";
            AuthorizeUrl = "https://idp.example.test/authorize";
            AccessTokenUrl = "https://idp.example.test/token";
            CallbackUrl = "https://app.example.test/auth/stuboauth2sync";
            RedirectUrl = "https://app.example.test/";
        }

        protected override string GetAccessTokenJson(string code)
        {
            AccessTokenExchangeCalls++;
            return "{\"access_token\":\"stub-access-token\"}";
        }

        protected override Dictionary<string, string> CreateAuthInfo(string accessToken)
        {
            return new Dictionary<string, string>
            {
                ["user_id"] = StubUserId,
                ["username"] = "stubusersync",
                ["name"] = "Stub User Sync",
                ["email"] = StubEmail,
            };
        }
    }

    private static string GetRedirectLocation(object result)
    {
        Assert.That(result, Is.InstanceOf<IHttpResult>());
        var httpResult = (IHttpResult)result;
        Assert.That(httpResult.Headers.TryGetValue(HttpHeaders.Location, out var location), Is.True);
        return location;
    }

    private static string ExtractQueryParam(string url, string key)
    {
        var query = url.RightPart('?');
        var parsed = PclExportClient.Instance.ParseQueryString(query);
        return parsed[key];
    }

    [Test]
    public async Task OAuth2_Callback_Without_State_Is_Rejected()
    {
        var provider = new StubOAuth2Provider(new AppSettings());
        using var appHost = new BasicAppHost
        {
            ConfigureAppHost = host => host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), [provider]))
        }.Init();

        var session = new AuthUserSession { Id = "victim-session-id", ReferrerUrl = "https://app.example.test/" };
        var req = new MockHttpRequest();
        req.QueryString["code"] = "attacker-code";
        req.Items[Keywords.Session] = session;

        var authService = new AuthenticateService { Request = req };
        var result = await provider.AuthenticateAsync(authService, session, new Authenticate { provider = provider.Provider });

        var location = GetRedirectLocation(result);
        Assert.That(location, Does.Contain("f=InvalidState"));
        Assert.That(provider.AccessTokenExchangeCalls, Is.EqualTo(0));
        Assert.That(session.IsAuthenticated, Is.False);
    }

    [Test]
    public async Task OAuth2_Callback_On_Authenticated_Session_Without_Stored_State_Does_Not_Link_Account()
    {
        var provider = new StubOAuth2Provider(new AppSettings());
        var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider)
        {
            AutoDisposeConnection = false
        };
        var authRepo = new OrmLiteAuthRepository(dbFactory);
        authRepo.InitSchema();

        using var appHost = new BasicAppHost
        {
            ConfigureAppHost = host =>
            {
                host.Container.Register<IAuthRepository>(authRepo);
                host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), [provider]));
            }
        }.Init();

        var victim = authRepo.CreateUserAuth(new UserAuth { UserName = "admin" }, "Password123!");

        var victimSession = new AuthUserSession
        {
            Id = "victim-session-id",
            UserAuthId = victim.Id.ToString(),
            UserAuthName = "admin",
            UserName = "admin",
            IsAuthenticated = true,
            ReferrerUrl = "https://app.example.test/",
        };

        var req = new MockHttpRequest();
        req.QueryString["code"] = "attacker-code";
        req.QueryString["state"] = "attacker-session-id";
        req.Items[Keywords.Session] = victimSession;

        var authService = new AuthenticateService { Request = req };
        var result = await provider.AuthenticateAsync(authService, victimSession, new Authenticate { provider = provider.Provider });

        var location = GetRedirectLocation(result);
        Assert.That(location, Does.Contain("f=InvalidState"));
        Assert.That(provider.AccessTokenExchangeCalls, Is.EqualTo(0));

        var reloadedVictim = authRepo.GetUserAuth(victim.Id.ToString());
        Assert.That(reloadedVictim.Email, Is.Null);
        Assert.That(authRepo.GetUserAuthDetails(victim.Id.ToString()), Is.Empty);
    }

    [Test]
    public async Task OAuth2_Callback_With_Mismatched_State_Is_Rejected()
    {
        var provider = new StubOAuth2Provider(new AppSettings());
        using var appHost = new BasicAppHost
        {
            ConfigureAppHost = host => host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), [provider]))
        }.Init();

        var session = new AuthUserSession { Id = "user-session-id", ReferrerUrl = "https://app.example.test/" };

        // Step 1: Pre-auth initiates flow and stores random state
        var preAuthReq = new MockHttpRequest();
        preAuthReq.Items[Keywords.Session] = session;
        var preAuthResult = await provider.AuthenticateAsync(new AuthenticateService { Request = preAuthReq }, session, new Authenticate { provider = provider.Provider });
        var preAuthLocation = GetRedirectLocation(preAuthResult);
        var state = ExtractQueryParam(preAuthLocation, Keywords.State);

        Assert.That(state, Is.Not.Null.And.Not.Empty);
        Assert.That(state, Is.Not.EqualTo(session.Id));
        Assert.That(session.Meta["oauthstate:stuboauth2"], Is.EqualTo(state));

        // Step 2: Callback with wrong state is rejected
        var badCallbackReq = new MockHttpRequest();
        badCallbackReq.QueryString["code"] = "valid-code";
        badCallbackReq.QueryString["state"] = "wrong-state";
        badCallbackReq.Items[Keywords.Session] = session;

        var badResult = await provider.AuthenticateAsync(new AuthenticateService { Request = badCallbackReq }, session, new Authenticate { provider = provider.Provider });
        Assert.That(GetRedirectLocation(badResult), Does.Contain("f=InvalidState"));
        Assert.That(provider.AccessTokenExchangeCalls, Is.EqualTo(0));
    }

    [Test]
    public async Task OAuth2_Valid_Flow_Succeeds_And_State_Cannot_Be_Replayed()
    {
        var provider = new StubOAuth2Provider(new AppSettings());
        using var appHost = new BasicAppHost
        {
            ConfigureAppHost = host => host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), [provider]))
        }.Init();

        var session = new AuthUserSession { Id = "user-session-id", ReferrerUrl = "https://app.example.test/" };

        // Step 1: Pre-auth
        var preAuthReq = new MockHttpRequest();
        preAuthReq.Items[Keywords.Session] = session;
        var preAuthResult = await provider.AuthenticateAsync(new AuthenticateService { Request = preAuthReq }, session, new Authenticate { provider = provider.Provider });
        var state = ExtractQueryParam(GetRedirectLocation(preAuthResult), Keywords.State);

        // Step 2: Valid callback
        var callbackReq = new MockHttpRequest();
        callbackReq.QueryString["code"] = "valid-code";
        callbackReq.QueryString["state"] = state;
        callbackReq.Items[Keywords.Session] = session;

        var callbackResult = await provider.AuthenticateAsync(new AuthenticateService { Request = callbackReq }, session, new Authenticate { provider = provider.Provider });
        Assert.That(GetRedirectLocation(callbackResult), Does.Contain("s=1"));
        Assert.That(provider.AccessTokenExchangeCalls, Is.EqualTo(1));
        Assert.That(session.IsAuthenticated, Is.True);
        Assert.That(session.Meta.ContainsKey("oauthstate:stuboauth2"), Is.False);

        // Step 3: Replay with same state is rejected
        var replayReq = new MockHttpRequest();
        replayReq.QueryString["code"] = "valid-code";
        replayReq.QueryString["state"] = state;
        replayReq.Items[Keywords.Session] = session;

        var replayResult = await provider.AuthenticateAsync(new AuthenticateService { Request = replayReq }, session, new Authenticate { provider = provider.Provider });
        Assert.That(GetRedirectLocation(replayResult), Does.Contain("f=InvalidState"));
        Assert.That(provider.AccessTokenExchangeCalls, Is.EqualTo(1));
    }

    [Test]
    public void OAuth2Sync_Rejects_Missing_And_Uninitiated_State_And_Prevents_Replay()
    {
        var provider = new StubOAuth2ProviderSync(new AppSettings());
        using var appHost = new BasicAppHost
        {
            ConfigureAppHost = host => host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), [provider]))
        }.Init();

        // Missing state
        var uninitiatedSession = new AuthUserSession { Id = "sync-session-1", ReferrerUrl = "https://app.example.test/" };
        var missingStateReq = new MockHttpRequest();
        missingStateReq.QueryString["code"] = "abc";
        missingStateReq.Items[Keywords.Session] = uninitiatedSession;
        var res1 = provider.Authenticate(new AuthenticateService { Request = missingStateReq }, uninitiatedSession, new Authenticate { provider = provider.Provider });
        Assert.That(GetRedirectLocation(res1), Does.Contain("f=InvalidState"));

        // Uninitiated session with arbitrary state
        var bogusStateReq = new MockHttpRequest();
        bogusStateReq.QueryString["code"] = "abc";
        bogusStateReq.QueryString["state"] = "forged";
        bogusStateReq.Items[Keywords.Session] = uninitiatedSession;
        var res2 = provider.Authenticate(new AuthenticateService { Request = bogusStateReq }, uninitiatedSession, new Authenticate { provider = provider.Provider });
        Assert.That(GetRedirectLocation(res2), Does.Contain("f=InvalidState"));
        Assert.That(provider.AccessTokenExchangeCalls, Is.EqualTo(0));

        // Valid flow
        var preAuthReq = new MockHttpRequest();
        preAuthReq.Items[Keywords.Session] = uninitiatedSession;
        var preAuthRes = provider.Authenticate(new AuthenticateService { Request = preAuthReq }, uninitiatedSession, new Authenticate { provider = provider.Provider });
        var state = ExtractQueryParam(GetRedirectLocation(preAuthRes), Keywords.State);

        var callbackReq = new MockHttpRequest();
        callbackReq.QueryString["code"] = "abc";
        callbackReq.QueryString["state"] = state;
        callbackReq.Items[Keywords.Session] = uninitiatedSession;
        var okRes = provider.Authenticate(new AuthenticateService { Request = callbackReq }, uninitiatedSession, new Authenticate { provider = provider.Provider });
        Assert.That(GetRedirectLocation(okRes), Does.Contain("s=1"));
        Assert.That(provider.AccessTokenExchangeCalls, Is.EqualTo(1));

        // Replay rejected
        var replayRes = provider.Authenticate(new AuthenticateService { Request = callbackReq }, uninitiatedSession, new Authenticate { provider = provider.Provider });
        Assert.That(GetRedirectLocation(replayRes), Does.Contain("f=InvalidState"));
        Assert.That(provider.AccessTokenExchangeCalls, Is.EqualTo(1));
    }

    [Test]
    public async Task OrmLiteAuthRepository_Does_Not_Reparent_Existing_OAuth_Link_To_Another_User()
    {
        var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider)
        {
            AutoDisposeConnection = false
        };
        var authRepo = new OrmLiteAuthRepository(dbFactory);
        authRepo.InitSchema();

        var userA = authRepo.CreateUserAuth(new UserAuth { UserName = "userA", Email = "a@example.test" }, "Password123!");
        var userB = authRepo.CreateUserAuth(new UserAuth { UserName = "userB" }, "Password123!");

        var oauthTokens = new AuthTokens
        {
            Provider = GoogleAuthProvider.Name,
            UserId = "google-123",
            Email = "a@example.test",
            DisplayName = "User A"
        };

        // Link google-123 to userA
        var sessionA = new AuthUserSession { UserAuthId = userA.Id.ToString(), UserAuthName = userA.UserName };
        var linked = await authRepo.CreateOrMergeAuthSessionAsync(sessionA, oauthTokens);
        Assert.That(linked.UserAuthId, Is.EqualTo(userA.Id));

        // Attempting to link the same (google, google-123) while signed in as userB must fail (async & sync)
        var sessionB = new AuthUserSession { UserAuthId = userB.Id.ToString(), UserAuthName = userB.UserName };
        Assert.ThrowsAsync<AccountAlreadyLinkedException>(async () =>
            await authRepo.CreateOrMergeAuthSessionAsync(sessionB, oauthTokens));
        Assert.Throws<AccountAlreadyLinkedException>(() =>
            authRepo.CreateOrMergeAuthSession(sessionB, oauthTokens));

        // Verify original link still belongs to userA and userB was not modified
        var detailsA = authRepo.GetUserAuthDetails(userA.Id.ToString());
        var detailsB = authRepo.GetUserAuthDetails(userB.Id.ToString());
        Assert.That(detailsA.Count, Is.EqualTo(1));
        Assert.That(detailsA[0].UserAuthId, Is.EqualTo(userA.Id));
        Assert.That(detailsB, Is.Empty);
        Assert.That(authRepo.GetUserAuth(userB.Id.ToString()).Email, Is.Null);
    }

    [Test]
    public async Task InMemoryAuthRepository_Does_Not_Reparent_Existing_OAuth_Link_To_Another_User()
    {
        var authRepo = new InMemoryAuthRepository();
        authRepo.Clear();

        var userA = authRepo.CreateUserAuth(new UserAuth { UserName = "userA", Email = "a@example.test" }, "Password123!");
        var userB = authRepo.CreateUserAuth(new UserAuth { UserName = "userB" }, "Password123!");

        var oauthTokens = new AuthTokens
        {
            Provider = GoogleAuthProvider.Name,
            UserId = "google-456",
            Email = "a@example.test",
            DisplayName = "User A"
        };

        var sessionA = new AuthUserSession { UserAuthId = userA.Id.ToString(), UserAuthName = userA.UserName };
        var linked = await authRepo.CreateOrMergeAuthSessionAsync(sessionA, oauthTokens);
        Assert.That(linked.UserAuthId, Is.EqualTo(userA.Id));

        var sessionB = new AuthUserSession { UserAuthId = userB.Id.ToString(), UserAuthName = userB.UserName };
        Assert.ThrowsAsync<AccountAlreadyLinkedException>(async () =>
            await authRepo.CreateOrMergeAuthSessionAsync(sessionB, oauthTokens));
        Assert.Throws<AccountAlreadyLinkedException>(() =>
            authRepo.CreateOrMergeAuthSession(sessionB, oauthTokens));

        Assert.That(authRepo.GetUserAuthDetails(userA.Id.ToString()).Count, Is.EqualTo(1));
        Assert.That(authRepo.GetUserAuthDetails(userB.Id.ToString()), Is.Empty);
    }

    [Test]
    public async Task OAuth2_Concurrent_Flows_With_Different_Providers_Do_Not_Overwrite_State()
    {
        var provider = new StubOAuth2Provider(new AppSettings());
        var otherProvider = new StubOAuth2Provider(new AppSettings()) { Provider = "otherstub" };
        using var appHost = new BasicAppHost
        {
            ConfigureAppHost = host => host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), [provider, otherProvider]))
        }.Init();

        var session = new AuthUserSession { Id = "user-session-id", ReferrerUrl = "https://app.example.test/" };

        var preAuthReq = new MockHttpRequest();
        preAuthReq.Items[Keywords.Session] = session;
        var state = ExtractQueryParam(GetRedirectLocation(
            await provider.AuthenticateAsync(new AuthenticateService { Request = preAuthReq }, session, new Authenticate { provider = provider.Provider })), Keywords.State);

        // Start a second flow with another provider before the first completes
        var otherPreAuthReq = new MockHttpRequest();
        otherPreAuthReq.Items[Keywords.Session] = session;
        await otherProvider.AuthenticateAsync(new AuthenticateService { Request = otherPreAuthReq }, session, new Authenticate { provider = otherProvider.Provider });

        var callbackReq = new MockHttpRequest();
        callbackReq.QueryString["code"] = "valid-code";
        callbackReq.QueryString["state"] = state;
        callbackReq.Items[Keywords.Session] = session;

        var callbackResult = await provider.AuthenticateAsync(new AuthenticateService { Request = callbackReq }, session, new Authenticate { provider = provider.Provider });
        Assert.That(GetRedirectLocation(callbackResult), Does.Contain("s=1"));
    }

    [Test]
    public async Task OAuth2_AccessToken_Auth_Is_Rejected_For_GET_Requests()
    {
        var verifyCalls = 0;
        var provider = new StubOAuth2Provider(new AppSettings())
        {
            VerifyAccessTokenAsync = (token, ctx) => { verifyCalls++; return Task.FromResult(true); }
        };
        using var appHost = new BasicAppHost
        {
            ConfigureAppHost = host => host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), [provider]))
        }.Init();

        var victimSession = new AuthUserSession
        {
            Id = "victim-session-id",
            UserAuthId = "1",
            IsAuthenticated = true,
            ReferrerUrl = "https://app.example.test/",
        };

        var req = new MockHttpRequest { HttpMethod = HttpMethods.Get };
        req.Items[Keywords.Session] = victimSession;

        var result = await provider.AuthenticateAsync(new AuthenticateService { Request = req }, victimSession,
            new Authenticate { provider = provider.Provider, AccessToken = "attacker-access-token" });

        Assert.That(result, Is.InstanceOf<HttpError>());
        Assert.That(((HttpError)result).StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Forbidden));
        Assert.That(verifyCalls, Is.EqualTo(0));
    }

    [Test]
    public async Task OAuth2_Callback_For_Identity_Linked_To_Another_User_Redirects_With_AccountAlreadyLinked()
    {
        var provider = new StubOAuth2Provider(new AppSettings());
        var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider)
        {
            AutoDisposeConnection = false
        };
        var authRepo = new OrmLiteAuthRepository(dbFactory);
        authRepo.InitSchema();

        using var appHost = new BasicAppHost
        {
            ConfigureAppHost = host =>
            {
                host.Container.Register<IAuthRepository>(authRepo);
                host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), [provider]) {
                    ValidateUniqueEmails = false,
                });
            }
        }.Init();

        var owner = authRepo.CreateUserAuth(new UserAuth { UserName = "owner" }, "Password123!");
        var other = authRepo.CreateUserAuth(new UserAuth { UserName = "other" }, "Password123!");
        await authRepo.CreateOrMergeAuthSessionAsync(new AuthUserSession { UserAuthId = owner.Id.ToString() },
            new AuthTokens { Provider = provider.Provider, UserId = provider.StubUserId });

        var session = new AuthUserSession
        {
            Id = "other-session-id",
            UserAuthId = other.Id.ToString(),
            UserAuthName = "other",
            IsAuthenticated = true,
            ReferrerUrl = "https://app.example.test/",
        };

        var preAuthReq = new MockHttpRequest();
        preAuthReq.Items[Keywords.Session] = session;
        var state = ExtractQueryParam(GetRedirectLocation(
            await provider.AuthenticateAsync(new AuthenticateService { Request = preAuthReq }, session, new Authenticate { provider = provider.Provider })), Keywords.State);

        var callbackReq = new MockHttpRequest();
        callbackReq.QueryString["code"] = "valid-code";
        callbackReq.QueryString["state"] = state;
        callbackReq.Items[Keywords.Session] = session;

        var result = await provider.AuthenticateAsync(new AuthenticateService { Request = callbackReq }, session, new Authenticate { provider = provider.Provider });
        Assert.That(GetRedirectLocation(result), Does.Contain("f=AccountAlreadyLinked"));
        Assert.That(authRepo.GetUserAuthDetails(owner.Id.ToString()).Count, Is.EqualTo(1));
        Assert.That(authRepo.GetUserAuthDetails(other.Id.ToString()), Is.Empty);

        // The already signed-in user isn't signed out
        var cachedSession = appHost.GetCacheClient().Get<AuthUserSession>(SessionFeature.GetSessionKey(session.Id));
        Assert.That(cachedSession, Is.Not.Null);
        Assert.That(cachedSession.UserAuthId, Is.EqualTo(other.Id.ToString()));
    }

    [Test]
    public async Task OAuth2_AccessToken_Auth_Rejects_CrossOrigin_POST_On_Authenticated_Session()
    {
        var verifyCalls = 0;
        var provider = new StubOAuth2Provider(new AppSettings())
        {
            VerifyAccessTokenAsync = (token, ctx) => { verifyCalls++; return Task.FromResult(true); }
        };
        using var appHost = new BasicAppHost
        {
            ConfigureAppHost = host => host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), [provider]))
        }.Init();

        var victimSession = new AuthUserSession
        {
            Id = "victim-session-id",
            UserAuthId = "1",
            IsAuthenticated = true,
            ReferrerUrl = "https://app.example.test/",
        };

        var req = new MockHttpRequest { HttpMethod = HttpMethods.Post };
        req.Headers[HttpHeaders.Origin] = "https://evil.example.test";
        req.Items[Keywords.Session] = victimSession;

        var result = await provider.AuthenticateAsync(new AuthenticateService { Request = req }, victimSession,
            new Authenticate { provider = provider.Provider, AccessToken = "attacker-access-token" });

        Assert.That(result, Is.InstanceOf<HttpError>());
        Assert.That(((HttpError)result).StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Forbidden));
        Assert.That(verifyCalls, Is.EqualTo(0));
    }

    private static OrmLiteAuthRepository CreateOrmLiteAuthRepo()
    {
        var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider)
        {
            AutoDisposeConnection = false
        };
        var authRepo = new OrmLiteAuthRepository(dbFactory);
        authRepo.InitSchema();
        return authRepo;
    }

    [Test]
    public async Task OAuth_Identity_Is_Not_Resolved_To_Existing_Account_By_Provider_UserName()
    {
        var authRepo = CreateOrmLiteAuthRepo();
        authRepo.CreateUserAuth(new UserAuth { UserName = "admin" }, "Password123!");

        // OAuth provider populates UserAuthName from the attacker chosen provider username
        var session = new AuthUserSession { UserAuthName = "admin" };
        var tokens = new AuthTokens { Provider = GithubAuthProvider.Name, UserId = "999", UserName = "admin" };

        Assert.That(authRepo.GetUserAuth(session, tokens), Is.Null);
        Assert.That(await authRepo.GetUserAuthAsync(session, tokens), Is.Null);

        // Non OAuth sessions can still resolve by UserAuthName
        Assert.That(authRepo.GetUserAuth(session, null)?.UserName, Is.EqualTo("admin"));
        Assert.That((await authRepo.GetUserAuthAsync(session, null))?.UserName, Is.EqualTo("admin"));
    }

    [Test]
    public async Task OAuth_Identity_Is_Only_Merged_By_Email_When_Email_Is_Verified()
    {
        var authRepo = CreateOrmLiteAuthRepo();
        var existing = authRepo.CreateUserAuth(new UserAuth { UserName = "user", Email = "user@example.test" }, "Password123!");

        var session = new AuthUserSession { UserAuthName = "user@example.test" };
        var unverified = new AuthTokens { Provider = "stuboauth2", UserId = "123", Email = "user@example.test" };
        Assert.That(authRepo.GetUserAuth(session, unverified), Is.Null);
        Assert.That(await authRepo.GetUserAuthAsync(session, unverified), Is.Null);

        var verified = new AuthTokens
        {
            Provider = "stuboauth2", UserId = "123", Email = "user@example.test",
            Items = new Dictionary<string, string> { ["email_verified"] = "true" },
        };
        Assert.That(authRepo.GetUserAuth(session, verified)?.Id, Is.EqualTo(existing.Id));
        Assert.That((await authRepo.GetUserAuthAsync(session, verified))?.Id, Is.EqualTo(existing.Id));

        var googleVerified = new AuthTokens
        {
            Provider = GoogleAuthProvider.Name, UserId = "456", Email = "user@example.test",
            Items = new Dictionary<string, string> { ["verified_email"] = "True" },
        };
        Assert.That(authRepo.GetUserAuth(session, googleVerified)?.Id, Is.EqualTo(existing.Id));
    }

    [Test]
    public void InMemoryAuthRepository_Does_Not_Resolve_OAuth_Identity_By_Provider_UserName()
    {
        var authRepo = new InMemoryAuthRepository();
        authRepo.Clear();
        authRepo.CreateUserAuth(new UserAuth { UserName = "admin" }, "Password123!");

        var session = new AuthUserSession { UserAuthName = "admin" };
        var tokens = new AuthTokens { Provider = GithubAuthProvider.Name, UserId = "999", UserName = "admin" };

        Assert.That(authRepo.GetUserAuth(session, tokens), Is.Null);
        Assert.That(authRepo.GetUserAuth(session, null)?.UserName, Is.EqualTo("admin"));
    }

    [Test]
    public async Task RestoreSessionFromState_Does_Not_Use_SessionId_As_State_Or_Restore_Session_From_State()
    {
        var provider = new StubOAuth2Provider(new AppSettings()) { RestoreSessionFromState = true };
        using var appHost = new BasicAppHost
        {
            ConfigureAppHost = host => host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), [provider]))
        }.Init();

        // Attacker starts a flow, their state must not be their session id
        var attackerSession = new AuthUserSession { Id = "attacker-session-id", ReferrerUrl = "https://app.example.test/" };
        var preAuthReq = new MockHttpRequest();
        preAuthReq.Items[Keywords.Session] = attackerSession;
        var attackerState = ExtractQueryParam(GetRedirectLocation(
            await provider.AuthenticateAsync(new AuthenticateService { Request = preAuthReq }, attackerSession, new Authenticate { provider = provider.Provider })), Keywords.State);
        Assert.That(attackerState, Is.Not.EqualTo(attackerSession.Id));

        // Victim opens a callback naming the attacker's session id as state, it must not load the attacker's session
        var victimSession = new AuthUserSession { Id = "victim-session-id", ReferrerUrl = "https://app.example.test/" };
        var callbackReq = new MockHttpRequest();
        callbackReq.QueryString["code"] = "attacker-code";
        callbackReq.QueryString["state"] = attackerSession.Id;
        callbackReq.QueryString[OAuthProvider.SessionCookieBounceParam] = "1";
        callbackReq.Items[Keywords.Session] = victimSession;

        var result = await provider.AuthenticateAsync(new AuthenticateService { Request = callbackReq }, victimSession, new Authenticate { provider = provider.Provider });
        Assert.That(GetRedirectLocation(result), Does.Contain("f=InvalidState"));
        Assert.That(provider.AccessTokenExchangeCalls, Is.EqualTo(0));
        Assert.That(victimSession.IsAuthenticated, Is.False);
    }

    [Test]
    public void SessionCookieBounce_Resends_GET_Callback_To_Same_Path_Once()
    {
        var req = new MockHttpRequest { HttpMethod = HttpMethods.Get, PathInfo = "/auth/google?code=abc&state=xyz" };
        req.QueryString["code"] = "abc";
        req.QueryString["state"] = "xyz";

        var bounce = OAuthProvider.CreateSessionCookieBounce(req);
        Assert.That(bounce, Is.Not.Null);
        var html = ((HttpResult)bounce).ResponseText;
        Assert.That(html, Does.Contain("/auth/google?code=abc"));
        Assert.That(html, Does.Contain(OAuthProvider.SessionCookieBounceParam + "=1"));
        Assert.That(html, Does.Not.Contain("http://localhost"));

        var bouncedReq = new MockHttpRequest { HttpMethod = HttpMethods.Get, PathInfo = "/auth/google" };
        bouncedReq.QueryString["code"] = "abc";
        bouncedReq.QueryString["state"] = "xyz";
        bouncedReq.QueryString[OAuthProvider.SessionCookieBounceParam] = "1";
        Assert.That(OAuthProvider.CreateSessionCookieBounce(bouncedReq), Is.Null);

        var startReq = new MockHttpRequest { HttpMethod = HttpMethods.Get, PathInfo = "/auth/google" };
        Assert.That(OAuthProvider.CreateSessionCookieBounce(startReq), Is.Null);
    }

    [Test]
    public void SessionCookieBounce_Resends_FormPost_Callback_With_Encoded_Fields()
    {
        var req = new MockHttpRequest { HttpMethod = HttpMethods.Post, PathInfo = "/auth/apple" };
        req.FormData["code"] = "abc";
        req.FormData["state"] = "xyz";
        req.FormData["user"] = "{\"name\":\"<script>\"}";

        var bounce = OAuthProvider.CreateSessionCookieBounce(req);
        var html = ((HttpResult)bounce).ResponseText;
        Assert.That(html, Does.Contain("method=\"post\""));
        Assert.That(html, Does.Contain("action=\"/auth/apple?" + OAuthProvider.SessionCookieBounceParam + "=1\""));
        Assert.That(html, Does.Contain("name=\"state\" value=\"xyz\""));
        Assert.That(html, Does.Not.Contain("<script>"));
        Assert.That(html, Does.Contain("&lt;script&gt;"));
    }

    private static async Task<object> AuthenticateWithAccessTokenFromOrigin(StubOAuth2Provider provider, AuthUserSession session, string origin)
    {
        var req = new MockHttpRequest { HttpMethod = HttpMethods.Post };
        if (origin != null)
            req.Headers[HttpHeaders.Origin] = origin;
        req.Items[Keywords.Session] = session;
        return await provider.AuthenticateAsync(new AuthenticateService { Request = req }, session,
            new Authenticate { provider = provider.Provider, AccessToken = "access-token" });
    }

    [Test]
    public async Task OAuth2_AccessToken_Auth_Rejects_CrossOrigin_POST_On_SignedOut_Session()
    {
        var verifyCalls = 0;
        var provider = new StubOAuth2Provider(new AppSettings())
        {
            VerifyAccessTokenAsync = (token, ctx) => { verifyCalls++; return Task.FromResult(true); }
        };
        using var appHost = new BasicAppHost
        {
            ConfigureAppHost = host => host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), [provider]))
        }.Init();

        // A cross-site form can't sign a signed-out victim in to the attacker's account
        var victimSession = new AuthUserSession { Id = "victim-session-id", ReferrerUrl = "https://app.example.test/" };
        var result = await AuthenticateWithAccessTokenFromOrigin(provider, victimSession, "https://evil.example.test");

        Assert.That(result, Is.InstanceOf<HttpError>());
        Assert.That(((HttpError)result).StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Forbidden));
        Assert.That(verifyCalls, Is.EqualTo(0));
        Assert.That(victimSession.IsAuthenticated, Is.False);
    }

    [TestCase(null, true)]                              // non-browser clients send no Origin
    [TestCase("http://localhost", true)]                // same host as the request
    [TestCase("https://localhost", true)]               // https origin behind a TLS-terminating proxy
    [TestCase("https://app.example.test", true)]        // HostConfig.WebHostUrl
    [TestCase("http://app.example.test", false)]        // WebHostUrl downgraded to http
    [TestCase("https://app.example.test:8443", false)]  // WebHostUrl on another port
    [TestCase("https://evil.example.test", false)]
    [TestCase("null", false)]
    public async Task OAuth2_AccessToken_Auth_Only_Allows_Trusted_Origins(string origin, bool allowed)
    {
        var provider = new StubOAuth2Provider(new AppSettings())
        {
            // Reject the token, so a request that passes the origin check returns 401 instead of 403
            VerifyAccessTokenAsync = (token, ctx) => Task.FromResult(false)
        };
        using var appHost = new BasicAppHost
        {
            ConfigureAppHost = host =>
            {
                host.Config.WebHostUrl = "https://app.example.test";
                host.Plugins.Add(new AuthFeature(() => new AuthUserSession(), [provider]));
            }
        }.Init();

        var session = new AuthUserSession { Id = "session-id", ReferrerUrl = "https://app.example.test/" };
        var result = await AuthenticateWithAccessTokenFromOrigin(provider, session, origin);

        Assert.That(result, Is.InstanceOf<HttpError>());
        Assert.That(((HttpError)result).StatusCode, Is.EqualTo(allowed
            ? System.Net.HttpStatusCode.Unauthorized
            : System.Net.HttpStatusCode.Forbidden));
    }

    [TestCase("//evil.example.test/auth/google")]
    [TestCase("///evil.example.test/auth/google")]
    [TestCase("/\\evil.example.test/auth/google")]
    public void SessionCookieBounce_Does_Not_Redirect_To_Another_Site(string path)
    {
        var req = new MockHttpRequest { HttpMethod = HttpMethods.Get, PathInfo = path };
        req.QueryString["code"] = "abc";
        req.QueryString["state"] = "xyz";

        var html = ((HttpResult)OAuthProvider.CreateSessionCookieBounce(req)).ResponseText;
        Assert.That(html, Does.Contain("/evil.example.test/auth/google"));
        Assert.That(html, Does.Not.Contain("//evil.example.test"));
        Assert.That(html, Does.Not.Contain("\\evil.example.test"));
    }

    [Test]
    public void SessionCookieBounce_FormPost_Submits_When_A_Field_Is_Named_Submit()
    {
        var req = new MockHttpRequest { HttpMethod = HttpMethods.Post, PathInfo = "/auth/apple" };
        req.FormData["code"] = "abc";
        req.FormData["state"] = "xyz";
        req.FormData["submit"] = "1";

        var html = ((HttpResult)OAuthProvider.CreateSessionCookieBounce(req)).ResponseText;
        Assert.That(html, Does.Contain("HTMLFormElement.prototype.submit.call(document.forms[0])"));
        Assert.That(html, Does.Not.Contain("document.forms[0].submit()"));
    }
}
