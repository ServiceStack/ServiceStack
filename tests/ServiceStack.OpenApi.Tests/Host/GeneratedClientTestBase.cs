using Funq;
using NUnit.Framework;
using ServiceStack.Api.OpenApi;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.Logging;
using ServiceStack.OpenApi.Tests.Services;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Text;
using System.Collections.Generic;

namespace ServiceStack.OpenApi.Tests.Host
{
    [TestFixture]
    public class GeneratedClientTestBase
    {
        TestAppHost appHost;

        [OneTimeSetUp]
        public void OnTestFixtureSetUp()
        {
            appHost = new TestAppHost();
            appHost.Init();
            appHost.Start(Config.AbsoluteBaseUri);
        }

        [OneTimeTearDown]
        public void OnTestFixtureTearDown()
        {
            appHost.Dispose();
        }
    }

    public class TestAppHost
        : AppSelfHostBase
    {
        //private static ILog log;

        public TestAppHost()
            : base("ServiceStack Autorest Client", typeof(NativeTypesTestService).Assembly)
        {
            //LogManager.LogFactory = new DebugLogFactory();
            //log = LogManager.GetLogger(typeof(ExampleAppHostHttpListener));
        }

        public override void Configure(Container container)
        {
            JsConfig.Init(new Text.Config { TextCase = TextCase.CamelCase });

            SetConfig(new HostConfig
            {
                DebugMode = true,
                Return204NoContentForEmptyResponse = true,
            });

            container.Register<IDbConnectionFactory>(c => new OrmLiteConnectionFactory(
                ":memory:", SqliteDialect.Provider));

            container.Register<IAuthRepository>(c =>
                new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>())
                {
                    UseDistinctRoleTables = AppSettings.Get("UseDistinctRoleTables", true),
                });

            var authRepo = (OrmLiteAuthRepository)container.Resolve<IAuthRepository>();
            authRepo.DropAndReCreateTables();

            CreateUser(authRepo, 1, "test", "test", new List<string> { "TheRole" }, new List<string> { "ThePermission" });
            CreateUser(authRepo, 2, "test2", "test2");

            Plugins.Add(new CorsFeature(
                allowOriginWhitelist: new[] { "http://localhost", "http://localhost:8080", "http://localhost:56500", "http://test.servicestack.net", "http://null.jsbin.com" },
                allowCredentials: true,
                allowedHeaders: "Content-Type, Allow, Authorization"));

            Plugins.Add(new AuthFeature(() => new CustomUserSession(),
                new IAuthProvider[]
                {
                    new BasicAuthProvider(AppSettings),
                    new CredentialsAuthProvider(AppSettings),
                }));

            Plugins.Add(new OpenApiFeature());

            /*Plugins.Add(new AutoQueryFeature
            {
                MaxLimit = 100,
            });

            container.RegisterValidators(typeof(ThrowValidationValidator).Assembly);

            JavaGenerator.AddGsonImport = true;
            var nativeTypes = this.GetPlugin<NativeTypesFeature>();
            nativeTypes.MetadataTypesConfig.ExportTypes.Add(typeof(DayOfWeek));
            


            this.RegisterRequestBinder<CustomRequestBinder>(
                httpReq => new CustomRequestBinder { IsFromBinder = true });

            Routes
                .Add<Movies>("/custom-movies", "GET")
                .Add<Movies>("/custom-movies/genres/{Genre}")
                .Add<Movie>("/custom-movies", "POST,PUT")
                .Add<Movie>("/custom-movies/{Id}")
                .Add<GetFactorial>("/fact/{ForNumber}")
                .Add<MoviesZip>("/all-movies.zip")
                .Add<GetHttpResult>("/gethttpresult")
            ;
            */
        }

        private void CreateUser(OrmLiteAuthRepository authRepo,
    int id, string username, string password, List<string> roles = null, List<string> permissions = null)
        {
            string hash;
            string salt;
            new SaltedHash().GetHashAndSaltString(password, out hash, out salt);

            authRepo.CreateUserAuth(new UserAuth
            {
                Id = id,
                DisplayName = username + " DisplayName",
                Email = username + "@gmail.com",
                UserName = username,
                FirstName = "First " + username,
                LastName = "Last " + username,
                PasswordHash = hash,
                Salt = salt,
                Roles = roles,
                Permissions = permissions
            }, password);

            authRepo.AssignRoles(id.ToString(), roles, permissions);
        }

    }
}
