using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ProtoBuf.Grpc.Client;
using ServiceStack.Auth;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace ServiceStack.Extensions.Tests.Protoc
{
    public class ProtocAuthTests
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
                : base(nameof(ProtocTests), typeof(MyServices).Assembly) { }

            public override void Configure(Container container)
            {
                RegisterService<GetFileService>();

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
                        IncludeJwtInConvertSessionToTokenResponse = true,
                        UseTokenCookie = false,
                    },
                    new ApiKeyAuthProvider(AppSettings) { RequireSecureConnection = false }
                ]));

                Plugins.Add(new RegistrationFeature());

                Plugins.Add(new GrpcFeature(App) {
                    CreateDynamicService = GrpcConfig.AutoQueryOrDynamicAttribute // required by protoc AutoQuery Tests
                });

                Plugins.Add(new ValidationFeature());
                Plugins.Add(new AutoQueryFeature());

                container.Register<IAuthRepository>(new InMemoryAuthRepository());
                container.Resolve<IAuthRepository>().InitSchema();

                GlobalRequestFilters.Add((req, res, dto) =>
                {
                    LastApiKey = req.GetApiKey();
                });
                
                AfterInitCallbacks.Add(host => {
                    
                    var authRepo = GetAuthRepository();
                    (authRepo as InMemoryAuthRepository)?.Clear();
                    
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
                
                ScriptContext.AddRequiredConfig();
            }

            public override void Configure(IServiceCollection services)
            {
                services.AddServiceStackGrpc();
            }

            public override void Configure(IApplicationBuilder app)
            {
                app.UseRouting();
            }

            public override void ConfigureKestrel(KestrelServerOptions options)
            {
                options.ListenLocalhost(TestsConfig.Port, listenOptions =>
                {
                    listenOptions.Protocols = HttpProtocols.Http2; // use for tests
                    // listenOptions.Protocols = HttpProtocols.Http1AndHttp2; // use for UpdateProto
                });
            }
        }

        // [Test] public void TestProtoTypes() => TestsConfig.BaseUri.CombineWith("/types/proto").GetStringFromUrl().Print();
        // [Test] // needs: listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
        public void UpdateProto()
        {
            Directory.GetCurrentDirectory().Print();
            var protoc = TestsConfig.BaseUri.CombineWith("/types/proto").GetStringFromUrl();
            protoc = protoc.Replace("ServiceStack.Extensions.Tests","ServiceStack.Extensions.Tests.Protoc");
            
            Directory.SetCurrentDirectory("../../../Protoc");
            File.WriteAllText("services.proto", protoc);
            ExecUtils.ShellExec("x proto-csharp services.proto");
        }

        private readonly ServiceStackHost appHost;
        public ProtocAuthTests()
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

        private static GrpcServices.GrpcServicesClient GetClient(Action<GrpcClientConfig> init = null) =>
            ProtocTests.GetClient(init);

        private async Task<string> GetRefreshToken()
        {
            var authClient = GetClient();
            
            var response = await authClient.PostAuthenticateAsync(new Authenticate
                {
                    Provider = "credentials",
                    UserName = Username,
                    Password = Password,
                });
            return response.RefreshToken;
        }
        
        protected virtual async Task<GrpcServices.GrpcServicesClient> GetClientWithRefreshToken(string refreshToken = null, string accessToken = null)
        {
            refreshToken ??= await GetRefreshToken();

            var client = GetClient(c => {
                c.RefreshToken = refreshToken;
                c.BearerToken = accessToken;
            });
            return client;
        }

        protected virtual GrpcServices.GrpcServicesClient GetClientWithBasicAuthCredentials()
        {
            var client = GetClient(c => {
                c.UserName = Username;
                c.Password = Password;
            });
            return client;
        }

        [Test]
        public async Task Can_not_access_Secured_without_Auth()
        {
            var client = GetClient();
            
            try
            {
                var request = new Secured { Name = "test" };
                var response = await client.PostSecuredAsync(request);
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

            var response = await client.PostSecuredAsync(request);
            Assert.That(response.Result, Is.EqualTo("Hello, test"));

            response = await client.PostSecuredAsync(request);
            Assert.That(response.Result, Is.EqualTo("Hello, test"));
        }
        
        [Test]
        public async Task Can_ConvertSessionToToken()
        {
            var authClient = GetClient();
            var authResponse = await authClient.PostAuthenticateAsync(new Authenticate
            {
                Provider = "credentials",
                UserName = Username,
                Password = Password,
            });
            Assert.That(authResponse.SessionId, Is.Not.Null);
            Assert.That(authResponse.UserName, Is.EqualTo(Username));
            Assert.That(authResponse.BearerToken, Is.Not.Null);

            authClient = GetClient(c => c.SessionId = authResponse.SessionId);

            var response = await authClient.PostHelloJwtAsync(new HelloJwt { Name = "from auth service" });
            Assert.That(response.Result, Is.EqualTo("Hello, from auth service"));

            GrpcClientConfig config = null;
            string bearerToken = null;
            authClient = GetClient(c => {
                bearerToken = authClient.PostConvertSessionToToken(new ConvertSessionToToken()).AccessToken;
                (config = c).BearerToken = bearerToken;
            });
            
            Assert.That(bearerToken, Is.Not.Null);
            
            config.SessionId = null;
            
            response = await authClient.PostHelloJwtAsync(new HelloJwt { Name = "from auth service" });
            Assert.That(response.Result, Is.EqualTo("Hello, from auth service"));
        }

        [Test]
        public async Task Invalid_RefreshToken_throws_RefreshTokenException()
        {
            var client = await GetClientWithRefreshToken("Invalid.Refresh.Token");
            try
            {
                var request = new Secured { Name = "test" };
                var response = await client.PostSecuredAsync(request);
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
            var response = await client.PostSecuredAsync(request);
            Assert.That(response.Result, Is.EqualTo("Hello, test"));

            response = await client.PostSecuredAsync(request);
            Assert.That(response.Result, Is.EqualTo("Hello, test"));
        }

        [Test]
        public async Task Can_Auto_reconnect_with_RefreshToken_after_expired_token()
        {
            var client = await GetClientWithRefreshToken(await GetRefreshToken(), CreateExpiredToken());

            var request = new Secured { Name = "test" };
            var response = await client.PostSecuredAsync(request);
            Assert.That(response.Result, Is.EqualTo("Hello, test"));

            response = await client.PostSecuredAsync(request);
            Assert.That(response.Result, Is.EqualTo("Hello, test"));
        }

        [Test]
        public void Can_Auto_reconnect_with_RefreshToken_after_expired_token_Sync()
        {
            var client = GetClientWithRefreshToken(GetRefreshToken().Result, CreateExpiredToken()).Result;

            var request = new Secured { Name = "test" };
            var response = client.PostSecured(request);
            Assert.That(response.Result, Is.EqualTo("Hello, test"));

            response = client.PostSecured(request);
            Assert.That(response.Result, Is.EqualTo("Hello, test"));
        }
        
        [Test]
        public async Task Does_return_token_on_subsequent_BasicAuth_Authentication_requests()
        {
            var client = GetClientWithBasicAuthCredentials();

            var response = await client.PostAuthenticateAsync(new Authenticate());
            Assert.That(response.BearerToken, Is.Not.Null);
            Assert.That(response.RefreshToken, Is.Not.Null);

            response = await client.PostAuthenticateAsync(new Authenticate());
            Assert.That(response.BearerToken, Is.Not.Null);
            Assert.That(response.RefreshToken, Is.Not.Null);
        }

        [Test]
        public async Task Can_Authenticate_with_ApiKey()
        {
            AppHost.LastApiKey = null;
            RequiresAuthService.LastApiKey = null;

            var client = GetClient(c => c.BearerToken = liveKey.Id);

            var request = new RequiresAuth { Name = "foo" };
            var response = await client.PostRequiresAuthAsync(request);
            Assert.That(response.Name, Is.EqualTo(request.Name));

            Assert.That(AppHost.LastApiKey.Id, Is.EqualTo(liveKey.Id));
            Assert.That(RequiresAuthService.LastApiKey.Id, Is.EqualTo(liveKey.Id));

            client = GetClient(c => c.BearerToken = testKey.Id);
            var testResponse = await client.PostSecuredAsync(new Secured { Name = "test" });
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
            var response = await client.PostRequiresAuthAsync(request);
            Assert.That(response.Name, Is.EqualTo(request.Name));

            Assert.That(AppHost.LastApiKey.Id, Is.EqualTo(liveKey.Id));
            Assert.That(RequiresAuthService.LastApiKey.Id, Is.EqualTo(liveKey.Id));
        }
        
    }
}