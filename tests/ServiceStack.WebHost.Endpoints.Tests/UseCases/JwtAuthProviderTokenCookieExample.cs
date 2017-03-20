using System.Reflection;
using Funq;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Text;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Data;
using ServiceStack.OrmLite;

namespace ServiceStack.WebHost.Endpoints.Tests.UseCases
{
    public class HelloJwt : IReturn<HelloJwtResponse>
    {
        public string Name { get; set; }
    }
    public class HelloJwtResponse
    {
        public string Result { get; set; }
    }

    [Authenticate]
    public class JwtServices : Service
    {
        public object Any(HelloJwt request)
        {
            return new HelloJwtResponse { Result = $"Hello, {request.Name}"};
        }
    }

    public class JwtAuthProviderTokenCookieExample
    {
        class AppHost : AppSelfHostBase
        {
            public AppHost() 
                : base(nameof(JwtAuthProviderTokenCookieExample), typeof(JwtServices).GetAssembly()) {}

            public override void Configure(Container container)
            {
                var dbFactory = new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider);

                container.Register<IDbConnectionFactory>(dbFactory);
                container.Register<IAuthRepository>(c =>
                    new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()) { UseDistinctRoleTables = true });

                //Create UserAuth RDBMS Tables
                container.Resolve<IAuthRepository>().InitSchema();

                //Also store User Sessions in SQL Server
                container.RegisterAs<OrmLiteCacheClient, ICacheClient>();
                container.Resolve<ICacheClient>().InitSchema();

                var privateKey = RsaUtils.CreatePrivateKeyParams(RsaKeyLengths.Bit2048);
                var publicKey = privateKey.ToPublicRsaParameters();
                var privateKeyXml = privateKey.ToPrivateKeyXml();
                var publicKeyXml = privateKey.ToPublicKeyXml();

                // just for testing, create a privateKeyXml on every instance
                Plugins.Add(new AuthFeature(() => new AuthUserSession(),
                    new IAuthProvider[]
                    {
                        new JwtAuthProvider {
                            HashAlgorithm = "RS256",
                            PrivateKeyXml = privateKeyXml,
                            RequireSecureConnection = false,
                        },
                        new CredentialsAuthProvider()
                    }));


                Plugins.Add(new RegistrationFeature());

                var authRepo = GetAuthRepository();
                authRepo.CreateUserAuth(new UserAuth
                {
                    Id = 1,
                    UserName = "Stefan",
                    FirstName = "First",
                    LastName = "Last",
                    DisplayName = "Display",
                }, "p@55word");
            }
        }

        private readonly ServiceStackHost appHost;

        public JwtAuthProviderTokenCookieExample()
        {
            appHost = new AppHost()
                .Init()
                .Start(Config.ListeningOn);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown() => appHost.Dispose();

        [Test]
        public void Can_get_TokenCookie()
        {
            var authClient = new JsonServiceClient(Config.ListeningOn);
            var authResponse = authClient.Send(new Authenticate
            {
                provider = "credentials",
                UserName = "Stefan",
                Password = "p@55word",
                UseTokenCookie = true
            });
            Assert.That(authResponse.SessionId, Is.Not.Null);
            Assert.That(authResponse.UserName, Is.EqualTo("Stefan"));

            var response = authClient.Send(new HelloJwt { Name = "from auth service" });
            Assert.That(response.Result, Is.EqualTo("Hello, from auth service"));

            var jwtToken = authClient.GetTokenCookie(); //From ss-tok Cookie
            Assert.That(jwtToken, Is.Not.Null);
        }

        [Test]
        public void Can_ConvertSessionToToken()
        {
            var authClient = new JsonServiceClient(Config.ListeningOn);
            var authResponse = authClient.Send(new Authenticate
            {
                provider = "credentials",
                UserName = "Stefan",
                Password = "p@55word",
                RememberMe = true,
            });
            Assert.That(authResponse.SessionId, Is.Not.Null);
            Assert.That(authResponse.UserName, Is.EqualTo("Stefan"));
            Assert.That(authResponse.BearerToken, Is.Not.Null);

            var jwtToken = authClient.GetTokenCookie(); //From ss-tok Cookie
            Assert.That(jwtToken, Is.Null);

            var response = authClient.Send(new HelloJwt { Name = "from auth service" });
            Assert.That(response.Result, Is.EqualTo("Hello, from auth service"));

            authClient.Send(new ConvertSessionToToken());
            jwtToken = authClient.GetTokenCookie(); //From ss-tok Cookie
            Assert.That(jwtToken, Is.Not.Null);

            response = authClient.Send(new HelloJwt { Name = "from auth service" });
            Assert.That(response.Result, Is.EqualTo("Hello, from auth service"));
        }

    }
}