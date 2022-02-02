using System;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ProtoBuf.Grpc.Client;
using ServiceStack.Auth;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.Extensions.Tests
{
    [DataContract]
    public class HelloJwt : IReturn<HelloJwtResponse>, IHasBearerToken
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }
        [DataMember(Order = 2)]
        public string BearerToken { get; set; }
    }
    [DataContract]
    public class HelloJwtResponse
    {
        [DataMember(Order = 1)]
        public string Result { get; set; }
        [DataMember(Order = 2)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [DataContract]
    public class Secured : IReturn<SecuredResponse>
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }
    }

    [DataContract]
    public class SecuredResponse
    {
        [DataMember(Order = 1)]
        public string Result { get; set; }

        [DataMember(Order = 2)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Authenticate]
    public class AuthServices : Service
    {
        public object Any(HelloJwt request)
        {
            return new HelloJwtResponse { Result = $"Hello, {request.Name}" };
        }

        public object Post(Secured request)
        {
            return new SecuredResponse { Result = $"Hello, {request.Name}" };
        }
    }
    
    [DataContract]
    public class RequiresAuth : IReturn<RequiresAuth>, IHasBearerToken
    {
        [DataMember(Order = 1)]
        public string Name { get; set; }
        [DataMember(Order = 2)]
        public string BearerToken { get; set; }
    }

    [Authenticate]
    public class RequiresAuthService : Service
    {
        public static ApiKey LastApiKey;

        public object Any(RequiresAuth request)
        {
            LastApiKey = base.Request.GetApiKey();
            return request;
        }
    }

    public class GrpcAuthTests
    {
        public static readonly byte[] AuthKey = AesUtils.CreateKey();
        public const string Username = "mythz";
        public const string Password = "p@55word";

        private static IManageApiKeys apiRepo;
        private const string userId = "1";
        private static ApiKey liveKey;
        private static ApiKey testKey;

        public class AppHost : AppSelfHostBase
        {
            public static ApiKey LastApiKey;
            public AppHost() 
                : base(nameof(GrpcTests), typeof(MyServices).Assembly) { }

            public override void Configure(Container container)
            {
                Plugins.Add(new ValidationFeature());
                Plugins.Add(new GrpcFeature(App));

                container.Register<IAuthRepository>(new InMemoryAuthRepository());
                container.Resolve<IAuthRepository>().InitSchema();

                Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                    new IAuthProvider[]
                    {
                        new BasicAuthProvider(),
                        new CredentialsAuthProvider(),
                        new JwtAuthProvider
                        {
                            AuthKey = AuthKey,
                            RequireSecureConnection = false,
                            AllowInQueryString = true,
                            AllowInFormData = true,
                            IncludeJwtInConvertSessionToTokenResponse = true,
                        },
                        new ApiKeyAuthProvider(AppSettings) { RequireSecureConnection = false },
                    }));

                Plugins.Add(new RegistrationFeature());

                GlobalRequestFilters.Add((req, res, dto) =>
                {
                    LastApiKey = req.GetApiKey();
                });
                
                AfterInitCallbacks.Add(host => {
                    
                    var authRepo = GetAuthRepository();
                    (authRepo as InMemoryAuthRepository).Clear();
                    authRepo.CreateUserAuth(new UserAuth
                    {
                        Id = userId.ToInt(),
                        UserName = Username,
                        FirstName = "First",
                        LastName = "Last",
                        DisplayName = "Display",
                    }, Password);

                    apiRepo = (IManageApiKeys)container.Resolve<IAuthRepository>();
                    var apiKeyProvider = (ApiKeyAuthProvider)AuthenticateService.GetAuthProvider(ApiKeyAuthProvider.Name);
                    var apiKeys = apiKeyProvider.GenerateNewApiKeys(userId);
                    using (authRepo as IDisposable)
                    {
                        apiRepo.StoreAll(apiKeys);
                    }
                    liveKey = apiKeys.First(x => x.Environment == "live");
                    testKey = apiKeys.First(x => x.Environment == "test");
                });
            }

            public override void ConfigureKestrel(KestrelServerOptions options)
            {
                options.ListenLocalhost(TestsConfig.Port, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2;
                });
            }

            public override void Configure(IServiceCollection services)
            {
                services.AddServiceStackGrpc();
            }

            public override void Configure(IApplicationBuilder app)
            {
                app.UseRouting();
            }
        }

        private readonly ServiceStackHost appHost;
        public GrpcAuthTests()
        {
            appHost = new AppHost()
                .Init()
                .Start(TestsConfig.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

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

        private async Task<string> GetRefreshToken()
        {
            var authClient = GetClient();
            var response = await authClient.SendAsync(new Authenticate
                {
                    provider = "credentials",
                    UserName = Username,
                    Password = Password,
                });
            return response.RefreshToken;
        }

        private static GrpcServiceClient GetClient() => TestsConfig.GetInsecureClient();

        protected virtual async Task<GrpcServiceClient> GetClientWithRefreshToken(string refreshToken = null, string accessToken = null)
        {
            if (refreshToken == null)
            {
                refreshToken = await GetRefreshToken();
            }

            var client = GetClient();
            client.RefreshToken = refreshToken;
            client.BearerToken = accessToken;
            return client;
        }

        protected virtual GrpcServiceClient GetClientWithBasicAuthCredentials()
        {
            var client = GetClient();
            client.SetCredentials(Username, Password);
            return client;
        }

        [Test]
        public async Task Can_not_access_Secured_without_Auth()
        {
            var client = GetClient();
            
            try
            {
                var request = new Secured { Name = "test" };
                var response = await client.SendAsync(request);
                Assert.Fail("Should throw");
            }
            catch (WebServiceException ex)
            {
                ex.Message.Print();
                Assert.That(ex.StatusCode, Is.EqualTo((int)HttpStatusCode.Unauthorized));
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(HttpStatusCode.Unauthorized)));
            }
        }

        [Test]
        public async Task Can_access_Secured_using_BasicAuth()
        {
            var client = GetClientWithBasicAuthCredentials();

            var request = new Secured { Name = "test" };

            var response = await client.SendAsync(request);
            Assert.That(response.Result, Is.EqualTo("Hello, test"));

            response = await client.PostAsync(request);
            Assert.That(response.Result, Is.EqualTo("Hello, test"));
        }
        
        [Test]
        public async Task Can_ConvertSessionToToken()
        {
            var authClient = GetClient();
            var authResponse = await authClient.SendAsync(new Authenticate
            {
                provider = "credentials",
                UserName = Username,
                Password = Password,
            });
            Assert.That(authResponse.SessionId, Is.Not.Null);
            Assert.That(authResponse.UserName, Is.EqualTo(Username));
            Assert.That(authResponse.BearerToken, Is.Not.Null);

            authClient.SessionId = authResponse.SessionId;

            var response = await authClient.SendAsync(new HelloJwt { Name = "from auth service" });
            Assert.That(response.Result, Is.EqualTo("Hello, from auth service"));

            authClient.BearerToken = (await authClient.SendAsync(new ConvertSessionToToken())).AccessToken;
            Assert.That(authClient.BearerToken, Is.Not.Null);

            authClient.SessionId = null;

            response = await authClient.SendAsync(new HelloJwt { Name = "from auth service" });
            Assert.That(response.Result, Is.EqualTo("Hello, from auth service"));
        }

        [Test]
        public async Task Invalid_RefreshToken_throws_RefreshTokenException()
        {
            var client = await GetClientWithRefreshToken("Invalid.Refresh.Token");
            try
            {
                var request = new Secured { Name = "test" };
                var response = await client.SendAsync(request);
                Assert.Fail("Should throw");
            }
            catch (RefreshTokenException ex)
            {
                ex.Message.Print();
                Assert.That(ex.ErrorCode, Is.EqualTo(nameof(ArgumentException)));
            }
        }

        [Test]
        public async Task Can_Auto_reconnect_with_just_RefreshToken()
        {
            var client = await GetClientWithRefreshToken();

            var request = new Secured { Name = "test" };
            var response = await client.SendAsync(request);
            Assert.That(response.Result, Is.EqualTo("Hello, test"));

            response = await client.SendAsync(request);
            Assert.That(response.Result, Is.EqualTo("Hello, test"));
        }

        [Test]
        public async Task Can_Auto_reconnect_with_RefreshToken_after_expired_token()
        {
            var client = await GetClientWithRefreshToken(await GetRefreshToken(), CreateExpiredToken());

            var request = new Secured { Name = "test" };
            var response = await client.SendAsync(request);
            Assert.That(response.Result, Is.EqualTo("Hello, test"));

            response = await client.SendAsync(request);
            Assert.That(response.Result, Is.EqualTo("Hello, test"));
        }
        
        [Test]
        public async Task Does_return_token_on_subsequent_BasicAuth_Authentication_requests()
        {
            var client = GetClientWithBasicAuthCredentials();

            var response = await client.PostAsync(new Authenticate());
            Assert.That(response.BearerToken, Is.Not.Null);
            Assert.That(response.RefreshToken, Is.Not.Null);

            response = await client.PostAsync(new Authenticate());
            Assert.That(response.BearerToken, Is.Not.Null);
            Assert.That(response.RefreshToken, Is.Not.Null);
        }

        [Test]
        public async Task Can_Authenticate_with_ApiKey()
        {
            AppHost.LastApiKey = null;
            RequiresAuthService.LastApiKey = null;

            var client = GetClient();
            client.BearerToken = liveKey.Id;

            var request = new RequiresAuth { Name = "foo" };
            var response = await client.SendAsync(request);
            Assert.That(response.Name, Is.EqualTo(request.Name));

            Assert.That(AppHost.LastApiKey.Id, Is.EqualTo(liveKey.Id));
            Assert.That(RequiresAuthService.LastApiKey.Id, Is.EqualTo(liveKey.Id));

            client.BearerToken = testKey.Id;
            var testResponse = await client.SendAsync(new Secured { Name = "test" });
            Assert.That(testResponse.Result, Is.EqualTo("Hello, test"));

            Assert.That(AppHost.LastApiKey.Id, Is.EqualTo(testKey.Id));
        }

        [Test]
        public async Task Does_allow_ApiKey_in_IHasBearerToken_RequestDto()
        {
            AppHost.LastApiKey = null;
            RequiresAuthService.LastApiKey = null;

            var client = GetClient();

            var request = new RequiresAuth { BearerToken = liveKey.Id, Name = "foo" };
            var response = await client.SendAsync(request);
            Assert.That(response.Name, Is.EqualTo(request.Name));

            Assert.That(AppHost.LastApiKey.Id, Is.EqualTo(liveKey.Id));
            Assert.That(RequiresAuthService.LastApiKey.Id, Is.EqualTo(liveKey.Id));
        }
    }
}