using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.OrmLite;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.UseCases
{
    
    public class HelloCustomSession : IReturn<HelloJwtSessionResponse>
    {
        
    }

    public class JwtCustomUserServices : Service
    {
        public object Any(HelloCustomSession request)
        {
            return new HelloJwtSessionResponse
            {
                Session = SessionAs<JwtAuthProviderCustomUserSessionTests.CustomJwtTestUserSession>()
            };
        }
    }

    public class HelloJwtSessionResponse
    {
        public JwtAuthProviderCustomUserSessionTests.CustomJwtTestUserSession Session { get; set; }
    }
    
    public class JwtAuthProviderCustomUserSessionTests
    {
        public const string Username = "mythz";
        public const string Password = "p@55word";
        
        public class CustomJwtTestUserSession : AuthUserSession
        {
            public string HostAddress { get; set; }

            public override async Task OnAuthenticatedAsync(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo,
                CancellationToken token = default)
            {
                await authService.SaveSessionAsync(session, token: token);
            }
        }
        
        public class AppUser : UserAuth
        {
            public string HostAddress { get; set; }
        }

        class AppHost : AppSelfHostBase
        {
            public AppHost()
                : base(nameof(JwtAuthProviderTests), typeof(JwtServices).Assembly)
            {
            }

            public virtual JwtAuthProvider JwtAuthProvider { get; set; }

            public override void Configure(Container container)
            {
                var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);

                container.Register<IDbConnectionFactory>(dbFactory);
                container.Register<IAuthRepository>(c =>
                    new OrmLiteAuthRepository<AppUser,UserAuthDetails>(c.Resolve<IDbConnectionFactory>())
                    {
                        UseDistinctRoleTables = true
                    });

                //Create UserAuth RDBMS Tables
                container.Resolve<IAuthRepository>().InitSchema();

                //Also store User Sessions in SQL Server
                container.RegisterAs<OrmLiteCacheClient, ICacheClient>();
                container.Resolve<ICacheClient>().InitSchema();

                // just for testing, create a privateKeyXml on every instance
                Plugins.Add(new AuthFeature(() => new CustomJwtTestUserSession(),
                    new IAuthProvider[]
                    {
                        new BasicAuthProvider(),
                        new CredentialsAuthProvider(),
                        JwtAuthProvider,
                    }));

                Plugins.Add(new RegistrationFeature());

                var authRepo = GetAuthRepository();
                authRepo.CreateUserAuth(new AppUser()
                {
                    Id = 1,
                    UserName = Username,
                    FirstName = "First3",
                    LastName = "Last2",
                    DisplayName = "Display1",
                    HostAddress = "test1234"
                }, Password);
            }
        }

        protected JwtAuthProvider CreateJwtAuthProvider()
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
                RequireSecureConnection = false
            };
        }

        protected virtual IJsonServiceClient GetClient() => new JsonServiceClient(Config.ListeningOn);
        
        private readonly ServiceStackHost appHost;

        public JwtAuthProviderCustomUserSessionTests()
        {
            appHost = new AppHost
                {
                    JwtAuthProvider = CreateJwtAuthProvider()
                }
                .Init()
                .Start(Config.ListeningOn);
        }
        
        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();
        
        [Test]
        public void Can_get_custom_session_property_when_using_jwt()
        {
            // Standard credentials auth, no token.
            var authClientWithoutTokenCookie = GetClient();
            var authResponse = authClientWithoutTokenCookie.Send(new Authenticate
            {
                provider = "credentials",
                UserName = Username,
                Password = Password,
                UseTokenCookie = false
            });
            Assert.That(authResponse.SessionId, Is.Not.Null);
            Assert.That(authResponse.UserName, Is.EqualTo(Username));

            var credentialsResponse = authClientWithoutTokenCookie.Send(new HelloCustomSession());

            Assert.That(credentialsResponse.Session.HostAddress, Is.Not.Null);
            
            // UseTokenCookie = true
            var authClientWithTokenCookie = GetClient();
            var authWithTokenResponse = authClientWithTokenCookie.Send(new Authenticate
            {
                provider = "credentials",
                UserName = Username,
                Password = Password,
                UseTokenCookie = true
            });
            Assert.That(authWithTokenResponse.SessionId, Is.Not.Null);
            Assert.That(authWithTokenResponse.UserName, Is.EqualTo(Username));

            var credentialsWithTokenResponse = authClientWithTokenCookie.Send(new HelloCustomSession());
            
            // Null
            Assert.That(credentialsWithTokenResponse.Session.HostAddress, Is.Not.Null);
            
            var jwtToken = authClientWithoutTokenCookie.GetTokenCookie(); //From ss-tok Cookie
            Assert.That(jwtToken, Is.Not.Null);
            
            var jwtClient = GetClient();
            jwtClient.BearerToken = jwtToken;

            var response = jwtClient.Send(new HelloCustomSession());

            Assert.That(response, Is.Not.Null);
            Assert.That(credentialsResponse, Is.Not.Null);
        }
    }
}