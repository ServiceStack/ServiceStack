using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.Host;
using ServiceStack.OrmLite;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests.UseCases;

public class JwtAuthProviderRsaEncryptedTests : JwtAuthProviderTests
{
    protected override JwtAuthProvider CreateJwtAuthProvider()
    {
        var privateKey = RsaUtils.CreatePrivateKeyParams(RsaKeyLengths.Bit2048);
        var publicKey = privateKey.ToPublicRsaParameters();
        var privateKeyXml = privateKey.ToPrivateKeyXml();
        var publicKeyXml = privateKey.ToPublicKeyXml();

        return new JwtAuthProvider
        {
            HashAlgorithm = "RS256",
            PrivateKeyXml = privateKeyXml,
            EncryptPayload = true,
            RequireSecureConnection = false,
        };
    }
}

public class JwtAuthProviderRsaTests : JwtAuthProviderTests
{
    protected override JwtAuthProvider CreateJwtAuthProvider()
    {
        var privateKey = RsaUtils.CreatePrivateKeyParams(RsaKeyLengths.Bit2048);
        var publicKey = privateKey.ToPublicRsaParameters();
        var privateKeyXml = privateKey.ToPrivateKeyXml();
        var publicKeyXml = privateKey.ToPublicKeyXml();

        return new JwtAuthProvider
        {
            HashAlgorithm = "RS256",
            PrivateKeyXml = privateKeyXml,
            RequireSecureConnection = false,
        };
    }
}

public class JwtAuthProviderHS256Tests : JwtAuthProviderTests
{
    private static readonly byte[] AuthKey = AesUtils.CreateKey();

    protected override JwtAuthProvider CreateJwtAuthProvider() => new() {
        AuthKey = AuthKey,
        RequireSecureConnection = false,
        AllowInQueryString = true,
        AllowInFormData = true,
    };

    [Test]
    public void Can_manually_create_an_authenticated_UserSession_in_Token()
    {
        var jwtProvider = CreateJwtAuthProvider();

        var header = JwtAuthProvider.CreateJwtHeader(jwtProvider.HashAlgorithm);
        var body = JwtAuthProvider.CreateJwtPayload(new AuthUserSession
            {
                UserAuthId = "1",
                DisplayName = "Test",
                Email = "as@if.com",
                IsAuthenticated = true,
            },
            issuer: jwtProvider.Issuer,
            expireIn: jwtProvider.ExpireTokensIn,
            audiences: jwtProvider.Audiences,
            roles: new[] {"TheRole"},
            permissions: new[] {"ThePermission"});

        var jwtToken = JwtAuthProvider.CreateJwt(header, body, jwtProvider.GetHashAlgorithm());

        var client = GetClient();

        try
        {
            client.Send(new HelloJwt { Name = "no jwt" });
            Assert.Fail("should throw");
        }
        catch (WebServiceException ex)
        {
            Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
        }

        client.SetTokenCookie(jwtToken);
        var response = client.Send(new HelloJwt { Name = "from Custom JWT" });
        Assert.That(response.Result, Is.EqualTo("Hello, from Custom JWT"));
    }

    [Test]
    public void Can_authenticate_using_JWT_with_QueryString()
    {
        var client = GetClientWithBasicAuthCredentials();

        var authResponse = client.Post(new Authenticate());
        AssertAuthenticateResponse(client, authResponse);

        var request = new Secured { Name = "test" };
        var url = Config.ListeningOn.CombineWith(request.ToGetUrl())
            .AddQueryParam(Keywords.TokenCookie, client.GetTokenCookie());

        var response = url.PostJsonToUrl("{}")
            .FromJson<SecuredResponse>();

        Assert.That(response.Result, Is.EqualTo(request.Name));
    }

    [Test]
    public void Requires_full_Signature_to_Authenticate()
    {
        var client = GetClientWithBasicAuthCredentials();

        var authResponse = client.Post(new Authenticate());
        AssertAuthenticateResponse(client, authResponse);

        var jwtProvider = (JwtAuthProvider) AuthenticateService.GetJwtAuthProvider();
        // Ensure minimum signature example
        // jwtProvider.ValidateToken = (js,req) => 
        //     req.GetJwtToken().LastRightPart('.').FromBase64UrlSafe().Length >= 32;

        var bearerToken = client.GetTokenCookie();
        var req = new BasicHttpRequest {
            Headers = {[HttpHeaders.Authorization] = "Bearer " + bearerToken}
        };

        Assert.That(jwtProvider.IsJwtValid(req));

        var startSigPos = bearerToken.LastIndexOf('.') + 1;
        for (var i = startSigPos; i < bearerToken.Length; i++)
        {
            req.Headers[HttpHeaders.Authorization] = "Bearer " + bearerToken.Substring(0, i);
            Assert.That(jwtProvider.IsJwtValid(req), Is.False);
        }
    }

    [Test]
    public void Can_authenticate_using_JWT_with_FormData()
    {
        var client = GetClientWithBasicAuthCredentials();

        var authResponse = client.Post(new Authenticate());
        AssertAuthenticateResponse(client, authResponse);

        var request = new Secured { Name = "test" };
        var url = Config.ListeningOn.CombineWith(request.ToGetUrl());

        var response = url.PostToUrl(new Dictionary<string,string> {
                { Keywords.TokenCookie, client.GetTokenCookie() }
            }, accept: MimeTypes.Json)
            .FromJson<SecuredResponse>();

        Assert.That(response.Result, Is.EqualTo(request.Name));
    }

    [Test]
    public void Can_authenticate_using_JWT_with_IHasBearerToken()
    {
        var authClient = GetClientWithBasicAuthCredentials();

        var authResponse = authClient.Post(new Authenticate());
        AssertAuthenticateResponse(authClient, authResponse);

        var client = GetClient();
        var request = new HelloJwt { BearerToken = authClient.GetTokenCookie(), Name = "IHasBearerToken" };
        var response = client.Get(request);

        Assert.That(response.Result, Is.EqualTo("Hello, IHasBearerToken"));
    }

    [Test]
    public void Does_escape_JWT_with_slashes()
    {
        var jwtHeader = new JsonObject {
            ["typ"] = "JWT",
            ["alg"] = "HS256",
        };
        var jwtPayload = new JsonObject {
            ["iss"] = "ssjwt",
            ["iat"] = "1635952233",
            ["exp"] = "1635955833",
            ["name"] = "Robin Doe",
            ["preferred_username"] = "domainname\\robindoe",
        };
            
        var jwtProvider = new JwtAuthProvider {
            AuthKey = AesUtils.CreateKey()
        };
        var jwt = JwtAuthProvider.CreateJwt(jwtHeader, jwtPayload, jwtProvider.GetHashAlgorithm());

        JsonObject validJwt = jwtProvider.GetVerifiedJwtPayload(jwt);
        Assert.That(validJwt["preferred_username"], Is.EqualTo("domainname\\robindoe"));
            
        var session = new AuthUserSession();
        session.PopulateFromMap(validJwt);
        Assert.That(session.UserName, Is.EqualTo("domainname\\robindoe"));
    }
}

public class JwtAuthProviderHS256HttpClientTests : JwtAuthProviderHS256Tests
{
    protected override IJsonServiceClient GetClient()
    {
        return new JsonHttpClient(Config.ListeningOn);
    }

    protected override IJsonServiceClient GetClientWithBasicAuthCredentials()
    {
        return new JsonHttpClient(Config.ListeningOn)
        {
            UserName = Username,
            Password = Password,
        };
    }
}

public abstract class JwtAuthProviderTests
{
    public const string Username = "mythz";
    public const string Password = "p@55word";

    class AppHost() : AppSelfHostBase(nameof(JwtAuthProviderTests), typeof(JwtServices).Assembly)
    {
        public virtual JwtAuthProvider JwtAuthProvider { get; set; }

        public override void Configure(Container container)
        {
            var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);

            container.Register<IDbConnectionFactory>(dbFactory);
            container.Register<IAuthRepository>(c =>
                new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>())
                {
                    UseDistinctRoleTables = true
                });

            //Create UserAuth RDBMS Tables
            container.Resolve<IAuthRepository>().InitSchema();

            //Also store User Sessions in SQL Server
            container.RegisterAs<OrmLiteCacheClient, ICacheClient>();
            container.Resolve<ICacheClient>().InitSchema();

            // just for testing, create a privateKeyXml on every instance
            Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                new IAuthProvider[]
                {
                    new BasicAuthProvider(),
                    new CredentialsAuthProvider(),
                    JwtAuthProvider,
                }));

            Plugins.Add(new RegistrationFeature());

            var authRepo = GetAuthRepository();
            authRepo.CreateUserAuth(new UserAuth
            {
                Id = 1,
                UserName = Username,
                FirstName = "First",
                LastName = "Last",
                DisplayName = "Display",
            }, Password);
        }
    }

    protected abstract JwtAuthProvider CreateJwtAuthProvider();

    protected virtual IJsonServiceClient GetClient() => new JsonServiceClient(Config.ListeningOn);

    protected virtual IJsonServiceClient GetClientWithBasicAuthCredentials() => new JsonServiceClient(Config.ListeningOn)
    {
        UserName = Username,
        Password = Password,
    };

    protected virtual IJsonServiceClient GetClientWithRefreshToken(string refreshToken = null, string accessToken = null, bool useTokenCookie = false)
    {
        if (refreshToken == null)
            refreshToken = GetRefreshToken();

        var client = GetClient();
        client.SetRefreshTokenCookie(refreshToken);
        if (!useTokenCookie)
            client.SetTokenCookie(accessToken);
        return client;
    }

    private static string CreateExpiredToken()
    {
        var jwtProvider = (JwtAuthProvider)AuthenticateService.GetAuthProvider(JwtAuthProviderReader.Name);
        jwtProvider.CreatePayloadFilter = (jwtPayload, session) =>
            jwtPayload["exp"] = DateTime.UtcNow.AddSeconds(-1).ToUnixTime().ToString();

        var token = jwtProvider.CreateJwtBearerToken(new AuthUserSession
        {
            UserAuthId = "1",
            DisplayName = "Test",
            Email = "as@if.com"
        });

        jwtProvider.CreatePayloadFilter = null;
        return token;
    }

    private readonly ServiceStackHost appHost;

    public JwtAuthProviderTests()
    {
        appHost = new AppHost
            {
                JwtAuthProvider = CreateJwtAuthProvider()
            }
            .Init()
            .Start(Config.ListeningOn);
    }

    protected static void AssertAuthenticateResponse(IJsonServiceClient client, AuthenticateResponse authResponse)
    {
        Assert.That(authResponse.BearerToken, Is.Null);
        Assert.That(client.GetTokenCookie(), Is.Not.Null);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    private string GetRefreshToken()
    {
        var authClient = GetClient();
        authClient.Send(new Authenticate {
            provider = "credentials",
            UserName = Username,
            Password = Password,
        });
        var refreshToken = authClient.GetRefreshTokenCookie();
        Assert.That(refreshToken, Is.Not.Null);
        return refreshToken;
    }

    [Test]
    public void Can_get_TokenCookie()
    {
        var authClient = GetClient();
        var authResponse = authClient.Send(new Authenticate
        {
            provider = "credentials",
            UserName = Username,
            Password = Password,
        });
        Assert.That(authResponse.SessionId, Is.Not.Null);
        Assert.That(authResponse.UserName, Is.EqualTo(Username));

        var response = authClient.Send(new HelloJwt { Name = "from auth service" });
        Assert.That(response.Result, Is.EqualTo("Hello, from auth service"));

        var jwtToken = authClient.GetTokenCookie(); //From ss-tok Cookie
        Assert.That(jwtToken, Is.Not.Null);
    }

    [Test]
    public void Can_ConvertSessionToToken()
    {
        var authClient = GetClient();
        var authResponse = authClient.Send(new Authenticate
        {
            provider = "credentials",
            UserName = Username,
            Password = Password,
            RememberMe = true,
        });
        Assert.That(authResponse.SessionId, Is.Not.Null);
        Assert.That(authResponse.UserName, Is.EqualTo(Username));
        Assert.That(authResponse.BearerToken, Is.Null);

        var jwtToken = authClient.GetTokenCookie(); //From ss-tok Cookie
        Assert.That(jwtToken, Is.Not.Null);

        var response = authClient.Send(new HelloJwt { Name = "from auth service" });
        Assert.That(response.Result, Is.EqualTo("Hello, from auth service"));

        authClient.Send(new ConvertSessionToToken());
        jwtToken = authClient.GetTokenCookie(); //From ss-tok Cookie
        Assert.That(jwtToken, Is.Not.Null);

        response = authClient.Send(new HelloJwt { Name = "from auth service" });
        Assert.That(response.Result, Is.EqualTo("Hello, from auth service"));
    }

    [Test]
    public void Invalid_RefreshToken_using_TokenCookies_throws_Unauthorized()
    {
        var client = GetClientWithRefreshToken("Invalid.Refresh.Token");
        try
        {
            var request = new Secured { Name = "test" };
            var response = client.Send(request);
            Assert.Fail("Should throw");
        }
        catch (WebServiceException ex)
        {
            ex.Message.Print();
            Assert.That(ex.StatusCode, Is.EqualTo(401));
            Assert.That(ex.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.Unauthorized)));
        }
    }

    [Test]
    public async Task Invalid_RefreshToken_using_TokenCookies_throws_Unauthorized_Async()
    {
        var client = GetClientWithRefreshToken("Invalid.Refresh.Token");
        try
        {
            var request = new Secured { Name = "test" };
            var response = await client.SendAsync(request);
            Assert.Fail("Should throw");
        }
        catch (WebServiceException ex)
        {
            ex.Message.Print();
            Assert.That(ex.StatusCode, Is.EqualTo(401));
            Assert.That(ex.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.Unauthorized)));
        }
    }

    [Test]
    public void Only_returns_Tokens_on_Requests_that_Authenticate_the_user()
    {
        var authClient = GetClient();
        var authResponse = authClient.Send(new Authenticate
        {
            provider = "credentials",
            UserName = Username,
            Password = Password,
        });

        Assert.That(authClient.GetRefreshTokenCookie(), Is.Not.Null); //On Auth using non IAuthWithRequest

        var postAuthRefreshToken = authClient.Send(new Authenticate()).RefreshToken;
        Assert.That(postAuthRefreshToken, Is.Null); //After Auth
    }

    [Test]
    public void Can_Auto_reconnect_with_just_RefreshToken()
    {
        var client = GetClientWithRefreshToken();

        var request = new Secured { Name = "test" };
        var response = client.Send(request);
        Assert.That(response.Result, Is.EqualTo(request.Name));

        response = client.Send(request);
        Assert.That(response.Result, Is.EqualTo(request.Name));
    }

    [Test]
    public void Can_Auto_reconnect_with_just_RefreshToken_with_UseTokenCookie()
    {
        var client = GetClientWithRefreshToken(useTokenCookie:true);

        var request = new Secured { Name = "test" };
        var response = client.Send(request);
        Assert.That(response.Result, Is.EqualTo(request.Name));

        response = client.Send(request);
        Assert.That(response.Result, Is.EqualTo(request.Name));
    }

    [Test]
    public void Can_Auto_reconnect_with_RefreshToken_after_expired_token()
    {
        var client = GetClientWithRefreshToken(GetRefreshToken(), CreateExpiredToken());

        var request = new Secured { Name = "test" };
        var response = client.Send(request);
        Assert.That(response.Result, Is.EqualTo(request.Name));

        response = client.Send(request);
        Assert.That(response.Result, Is.EqualTo(request.Name));
    }

    [Test]
    public async Task Can_Auto_reconnect_with_RefreshToken_after_expired_token_Async()
    {
        var client = GetClientWithRefreshToken(GetRefreshToken(), CreateExpiredToken());

        var request = new Secured { Name = "test" };
        var response = await client.SendAsync(request);
        Assert.That(response.Result, Is.EqualTo(request.Name));

        response = await client.SendAsync(request);
        Assert.That(response.Result, Is.EqualTo(request.Name));
    }

    [Test]
    public void Can_Auto_reconnect_with_RefreshToken_in_OnAuthenticationRequired_after_expired_token()
    {
        var client = GetClient();
        if (!(client is JsonServiceClient serviceClient)) //OnAuthenticationRequired not implemented in JsonHttpClient
            return;

        var called = 0;
        serviceClient.BearerToken = CreateExpiredToken();

        serviceClient.OnAuthenticationRequired = () =>
        {
            called++;
            var authClient = GetClient();
            var authResponse = authClient.Send(new GetAccessToken {
                RefreshToken = GetRefreshToken(),
            });
            serviceClient.BearerToken = authClient.GetTokenCookie();
        };

        var request = new Secured { Name = "test" };
        var response = client.Send(request);
        Assert.That(response.Result, Is.EqualTo(request.Name));

        response = client.Send(request);
        Assert.That(response.Result, Is.EqualTo(request.Name));

        Assert.That(called, Is.EqualTo(1));
    }

    [Test]
    public void Does_return_token_on_subsequent_BasicAuth_Authentication_requests()
    {
        var client = GetClientWithBasicAuthCredentials();

        var response = client.Post(new Authenticate());
        Assert.That(client.GetTokenCookie(), Is.Not.Null);
        Assert.That(client.GetRefreshTokenCookie(), Is.Not.Null);

        response = client.Post(new Authenticate());
        Assert.That(client.GetTokenCookie(), Is.Not.Null);
        Assert.That(client.GetRefreshTokenCookie(), Is.Not.Null);
    }

    [Test]
    public async Task Does_return_token_on_subsequent_BasicAuth_Authentication_requests_Async()
    {
        var client = GetClientWithBasicAuthCredentials();

        var response = await client.PostAsync(new Authenticate());
        Assert.That(client.GetTokenCookie(), Is.Not.Null);
        Assert.That(client.GetRefreshTokenCookie(), Is.Not.Null);

        response = await client.PostAsync(new Authenticate());
        Assert.That(client.GetTokenCookie(), Is.Not.Null);
        Assert.That(client.GetRefreshTokenCookie(), Is.Not.Null);
    }

    [Test]
    public void Does_return_token_on_subsequent_Credentials_Authentication_requests()
    {
        var client = GetClient();

        var response = client.Post(new Authenticate
        {
            provider = "credentials",
            UserName = Username,
            Password = Password,
            RememberMe = true,
        });
        Assert.That(client.GetTokenCookie(), Is.Not.Null);
        Assert.That(client.GetRefreshTokenCookie(), Is.Not.Null);

        response = client.Post(new Authenticate
        {
            provider = "credentials",
            UserName = Username,
            Password = Password,
            RememberMe = true,
        });
        Assert.That(client.GetTokenCookie(), Is.Not.Null);
        Assert.That(client.GetRefreshTokenCookie(), Is.Not.Null);

        response = client.Post(new Authenticate());
        Assert.That(client.GetTokenCookie(), Is.Not.Null);
        Assert.That(client.GetRefreshTokenCookie(), Is.Not.Null);
    }

    [Test]
    public void Can_validate_valid_token()
    {
        var authClient = GetClient();
        authClient.Send(new Authenticate
        {
            provider = "credentials",
            UserName = Username,
            Password = Password,
        });

        var jwtProvider = AuthenticateService.GetJwtAuthProvider();
        var jwt = authClient.GetTokenCookie();
        Assert.That(jwtProvider.IsJwtValid(jwt));

        var jwtPayload = jwtProvider.GetValidJwtPayload(jwt);
        Assert.That(jwtPayload, Is.Not.Null);
        Assert.That(jwtPayload["preferred_username"], Is.EqualTo(Username));
    }

    [Test]
    public void Does_not_validate_invalid_token()
    {
        var expiredJwt = CreateExpiredToken();

        var jwtProvider = AuthenticateService.GetJwtAuthProvider();
        Assert.That(jwtProvider.IsJwtValid(expiredJwt), Is.False);

        Assert.That(jwtProvider.GetValidJwtPayload(expiredJwt), Is.Null);
    }

    [Test]
    public void Does_validate_multiple_audiences()
    {
        var jwtProvider = (JwtAuthProvider)AuthenticateService.GetAuthProvider(JwtAuthProviderReader.Name);

        string CreateJwtWithAudiences(params string[] audiences)
        {
            var header = JwtAuthProvider.CreateJwtHeader(jwtProvider.HashAlgorithm);
            var body = JwtAuthProvider.CreateJwtPayload(new AuthUserSession
                {
                    UserAuthId = "1",
                    DisplayName = "Test",
                    Email = "as@if.com",
                    IsAuthenticated = true,
                },
                issuer: jwtProvider.Issuer,
                expireIn: jwtProvider.ExpireTokensIn,
                audiences: audiences);

            var jwtToken = JwtAuthProvider.CreateJwt(header, body, jwtProvider.GetHashAlgorithm());
            return jwtToken;
        }

        jwtProvider.Audiences = new List<string> { "foo", "bar" };
        var jwtNoAudience = CreateJwtWithAudiences();
        Assert.That(jwtProvider.IsJwtValid(jwtNoAudience));

        var jwtWrongAudience = CreateJwtWithAudiences("qux");
        Assert.That(!jwtProvider.IsJwtValid(jwtWrongAudience));
            
        var jwtPartialAudienceMatch = CreateJwtWithAudiences("bar","qux");
        Assert.That(jwtProvider.IsJwtValid(jwtPartialAudienceMatch));

        jwtProvider.Audience = "foo";
        Assert.That(!jwtProvider.IsJwtValid(jwtPartialAudienceMatch));

        jwtProvider.Audience = null;
        Assert.That(jwtProvider.IsJwtValid(jwtPartialAudienceMatch));
    }

    [Test]
    public void Can_handle_roles_with_spaces()
    {
        var jwtProvider = (JwtAuthProvider)AuthenticateService.GetAuthProvider(JwtAuthProviderReader.Name);
        var header = JwtAuthProvider.CreateJwtHeader(jwtProvider.HashAlgorithm);
        var roles = new[] { "Super Admin" }; 
        var perms = new[] { "Perm", "The Perm", "Super Perm"}; 
        var body = JwtAuthProvider.CreateJwtPayload(new AuthUserSession
            {
                UserAuthId = "1",
                DisplayName = "Test",
                Email = "as@if.com",
                IsAuthenticated = true,
            },
            issuer: jwtProvider.Issuer,
            expireIn: jwtProvider.ExpireTokensIn,
            roles:roles,
            permissions:perms);

        var jwtToken = JwtAuthProvider.CreateJwt(header, body, jwtProvider.GetHashAlgorithm());
        JwtAuthProvider.Dump(jwtToken).Print();

        var payload = JwtAuthProvider.ExtractPayload(jwtToken);
        Assert.That(payload[JwtClaimTypes.Roles], Is.EquivalentTo(roles));
        Assert.That(payload[JwtClaimTypes.Permissions], Is.EquivalentTo(perms));
        
        var jsonObj = jwtProvider.GetVerifiedJwtPayload(jwtToken);
        Assert.That(jsonObj.GetUnescaped(JwtClaimTypes.Roles), Is.EquivalentTo("[\"Super Admin\"]"));
        Assert.That(jsonObj.GetUnescaped(JwtClaimTypes.Permissions), Is.EquivalentTo("[\"Perm\",\"The Perm\",\"Super Perm\"]"));

        var session = new AuthUserSession();
        session.PopulateFromMap(jsonObj);
        Assert.That(session.Roles, Is.EquivalentTo(roles));
        Assert.That(session.Permissions, Is.EquivalentTo(perms));
    }

}
    
public class JwtAuthProviderIntegrationTests
{
    public const string Username = "mythz";
    public const string Password = "p@55word";
    private static readonly byte[] AuthKey = AesUtils.CreateKey();

    private readonly ServiceStackHost appHost;

    public JwtAuthProviderIntegrationTests()
    {
        appHost = new AppHost()
            .Init()
            .Start(Config.ListeningOn);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    class AppHost : AppSelfHostBase
    {
        public AppHost()
            : base(nameof(JwtAuthProviderIntegrationTests), typeof(JwtServices).Assembly) { }

        public override void Configure(Container container)
        {
            // just for testing, create a privateKeyXml on every instance
            Plugins.Add(new AuthFeature(() => new AuthUserSession(),
            [
                new BasicAuthProvider(),
                    new CredentialsAuthProvider(),
                    new JwtAuthProvider
                    {
                        AuthKey = AuthKey,
                        RequireSecureConnection = false,
                        AllowInQueryString = true,
                        AllowInFormData = true,
                    }
            ]));

            Plugins.Add(new RegistrationFeature());

            container.Register<IAuthRepository>(c => new InMemoryAuthRepository());

            var authRepo = GetAuthRepository();
            authRepo.CreateUserAuth(new UserAuth
            {
                Id = 1,
                UserName = Username,
                FirstName = "First",
                LastName = "Last",
                DisplayName = "Display",
            }, Password);
                
            Plugins.Add(new RequestLogsFeature {
                EnableSessionTracking = true,
                ExcludeRequestDtoTypes = { typeof(Authenticate) },
            });
        }
    }

    protected virtual IJsonServiceClient GetClient() => new JsonServiceClient(Config.ListeningOn) {
        UserName = Username,
        Password = Password,
    };

    protected virtual IJsonServiceClient GetJwtClient()
    {
        var authClient = GetClient();
        var authResponse = authClient.Post(new Authenticate());
        return new JsonServiceClient(Config.ListeningOn) {
            BearerToken = authClient.GetTokenCookie()
        };
    }

    [Test]
    public void Does_track_JWT_Sessions_calling_Authenticate_Services()
    {
        var client = GetJwtClient();

        var request = new Secured { Name = "test" };
        var response = client.Send(request);
        Assert.That(response.Result, Is.EqualTo(request.Name));

        var reqLogger = HostContext.TryResolve<IRequestLogger>();
        var lastEntrySession = reqLogger.GetLatestLogs(1)[0]?.Session as AuthUserSession;
        Assert.That(lastEntrySession, Is.Not.Null);
        Assert.That(lastEntrySession.AuthProvider, Is.EqualTo("jwt"));
        Assert.That(lastEntrySession.UserName, Is.EqualTo(Username));
    }

    [Test]
    public void Does_track_JWT_Sessions_calling_non_Authenticate_Services()
    {
        var client = GetJwtClient();

        var request = new Unsecure { Name = "test" };
        var response = client.Send(request);
        Assert.That(response.Name, Is.EqualTo(request.Name));

        var reqLogger = HostContext.TryResolve<IRequestLogger>();
        var lastEntrySession = reqLogger.GetLatestLogs(1)[0]?.Session as AuthUserSession;
        Assert.That(lastEntrySession, Is.Not.Null);
        Assert.That(lastEntrySession.AuthProvider, Is.EqualTo("jwt"));
        Assert.That(lastEntrySession.UserName, Is.EqualTo(Username));
    }
}
    
public class JwtAuthProviderTokenCookieTests
{
    public const string Username = "ss-reftok";
    public const string Password = "p@55word";
    private static readonly byte[] AuthKey = AesUtils.CreateKey();

    private readonly ServiceStackHost appHost;

    public JwtAuthProviderTokenCookieTests()
    {
        appHost = new AppHost()
            .Init()
            .Start(Config.ListeningOn);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();

    class AppHost : AppSelfHostBase
    {
        public AppHost()
            : base(nameof(JwtAuthProviderTokenCookieTests), typeof(JwtServices).Assembly) { }

        public override void Configure(Container container)
        {
            // just for testing, create a privateKeyXml on every instance
            Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                new IAuthProvider[] {
                    new CredentialsAuthProvider(),
                    new JwtAuthProvider
                    {
                        AuthKey = AuthKey,
                        RequireSecureConnection = false,
                        UseTokenCookie = true,
                        AllowInQueryString = true,
                        AllowInFormData = true,
                    },
                }));

            container.Register<IAuthRepository>(c => new InMemoryAuthRepository());

            var authRepo = GetAuthRepository();
            authRepo.CreateUserAuth(new UserAuth
            {
                Id = 1,
                UserName = Username,
                FirstName = "First",
                LastName = "Last",
                DisplayName = "Display",
            }, Password);
                
            Plugins.Add(new RequestLogsFeature {
                EnableSessionTracking = true,
                ExcludeRequestDtoTypes = { typeof(Authenticate) },
            });
        }
    }

    [Test]
    public void Can_use_RefreshTokenCookie_to_authenticate_and_get_new_AccessToken()
    {
        string initialAccessToken = null;
        var client = new JsonServiceClient(Config.ListeningOn) {
            ResponseFilter = res => {
                if (initialAccessToken == null)
                {
                    var accessToken = res.Cookies[Keywords.TokenCookie];
                    Assert.That(accessToken.Value, Is.Not.Null);
                    initialAccessToken = accessToken.Value;
                    var refreshToken = res.Cookies[Keywords.RefreshTokenCookie];
                    Assert.That(refreshToken.Value, Is.Not.Null);
                }
            }
        };
        var authResponse = client.Post(new Authenticate {
            provider = "credentials",
            UserName = Username,
            Password = Password
        });

        var request = new Secured { Name = "test" };
        var response = client.Send(request);
        Assert.That(response.Result, Is.EqualTo(request.Name));

        var reqLogger = HostContext.TryResolve<IRequestLogger>();
        var lastEntrySession = reqLogger.GetLatestLogs(1)[0]?.Session as AuthUserSession;
        Assert.That(lastEntrySession, Is.Not.Null);
        Assert.That(lastEntrySession.AuthProvider, Is.EqualTo("jwt"));
        Assert.That(lastEntrySession.UserName, Is.EqualTo(Username));

        string lastAccessToken = null;
        client.ResponseFilter = res => {
            var accessToken = res.Cookies[Keywords.TokenCookie];
            lastAccessToken = accessToken.Value;
        };
        var i = 0;
        do
        {
            var accessTokenResponse = client.Post(new GetAccessToken());
            ExecUtils.SleepBackOffMultiplier(++i); //need to wait for iat to tick +1s so JWT's are different
        } 
        while (lastAccessToken == initialAccessToken);
    }

    [Test]
    public void Does_auto_fetch_new_AccessToken_with_RefreshTokenCookie_ServiceClient() => 
        AssertDoesGetAccessTokenUsingRefreshTokenCookie(new JsonServiceClient(Config.ListeningOn));

    [Test]
    public void Does_auto_fetch_new_AccessToken_with_RefreshTokenCookie_HttpClient() => 
        AssertDoesGetAccessTokenUsingRefreshTokenCookie(new JsonHttpClient(Config.ListeningOn));

    private static void AssertDoesGetAccessTokenUsingRefreshTokenCookie(IJsonServiceClient client)
    {
        var authResponse = client.Post(new Authenticate {
            provider = "credentials",
            UserName = Username,
            Password = Password
        });

        var initialAccessToken = client.GetTokenCookie();
        var initialRefreshToken = client.GetRefreshTokenCookie();
        Assert.That(initialAccessToken, Is.Not.Null);
        Assert.That(initialRefreshToken, Is.Not.Null);

        var request = new Secured {Name = "test"};
        var response = client.Send(request);
        Assert.That(response.Result, Is.EqualTo(request.Name));

        var jwtAuthProvider = AuthenticateService.GetRequiredJwtAuthProvider();
        jwtAuthProvider.InvalidateJwtIds.Add(jwtAuthProvider.LastJwtId());
        // JwtAuthProvider.PrintDump(initialAccessToken);
        // JwtAuthProvider.PrintDump(initialRefreshToken);

        response = client.Send(request);
        Assert.That(response.Result, Is.EqualTo(request.Name));
        var latestAccessToken = client.GetTokenCookie();
        Assert.That(latestAccessToken, Is.Not.EqualTo(initialAccessToken));
    }

    [Test]
    public async Task Does_auto_fetch_new_AccessToken_with_RefreshTokenCookie_ServiceClient_Async() => 
        await AssertDoesGetAccessTokenUsingRefreshTokenCookieAsync(new JsonServiceClient(Config.ListeningOn));

    [Test]
    public async Task Does_auto_fetch_new_AccessToken_with_RefreshTokenCookie_HttpClient_Async() => 
        await AssertDoesGetAccessTokenUsingRefreshTokenCookieAsync(new JsonHttpClient(Config.ListeningOn));

    private static async Task AssertDoesGetAccessTokenUsingRefreshTokenCookieAsync(IJsonServiceClient client)
    {
        var authResponse = await client.PostAsync(new Authenticate {
            provider = "credentials",
            UserName = Username,
            Password = Password
        });

        var initialAccessToken = client.GetTokenCookie();
        var initialRefreshToken = client.GetRefreshTokenCookie();
        Assert.That(initialAccessToken, Is.Not.Null);
        Assert.That(initialRefreshToken, Is.Not.Null);

        var request = new Secured {Name = "test"};
        var response = await client.SendAsync(request);
        Assert.That(response.Result, Is.EqualTo(request.Name));

        var jwtAuthProvider = AuthenticateService.GetRequiredJwtAuthProvider();
        jwtAuthProvider.InvalidateJwtIds.Add(jwtAuthProvider.LastJwtId());
        // JwtAuthProvider.PrintDump(initialAccessToken);
        // JwtAuthProvider.PrintDump(initialRefreshToken);

        response = await client.SendAsync(request);
        Assert.That(response.Result, Is.EqualTo(request.Name));
        var latestAccessToken = client.GetTokenCookie();
        Assert.That(latestAccessToken, Is.Not.EqualTo(initialAccessToken));
    }
}