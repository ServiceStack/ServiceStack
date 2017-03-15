using Funq;
using NUnit.Framework;
using ServiceStack.Api.Swagger2;
using ServiceStack.Auth;
using ServiceStack.Logging;
using ServiceStack.OpenApi.Tests.Services;
using ServiceStack.Redis;
using ServiceStack.Text;

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
            : base("ServiceStack Autorest Client", typeof(NativeTypesTestService).GetAssembly())
        {
            //LogManager.LogFactory = new DebugLogFactory();
            //log = LogManager.GetLogger(typeof(ExampleAppHostHttpListener));
        }

        public override void Configure(Container container)
        {
            JsConfig.EmitCamelCaseNames = true;

            SetConfig(new HostConfig
            {
                DebugMode = true,
                Return204NoContentForEmptyResponse = true,
            });

/*            container.Register<IRedisClientsManager>(c =>
                new RedisManagerPool("localhost:6379"));
            container.Register(c => c.Resolve<IRedisClientsManager>().GetCacheClient());

            container.Register<IDbConnectionFactory>(c => new OrmLiteConnectionFactory(
                AppSettings.GetString("AppDb"), PostgreSqlDialect.Provider));

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<Logger>();
            }

            container.Register<IAuthRepository>(c =>
                new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>())
                {
                    UseDistinctRoleTables = AppSettings.Get("UseDistinctRoleTables", true),
                });

            var authRepo = (OrmLiteAuthRepository)container.Resolve<IAuthRepository>();
            authRepo.DropAndReCreateTables();

            CreateUser(authRepo, 1, "test", "test", new List<string> { "TheRole" }, new List<string> { "ThePermission" });
            CreateUser(authRepo, 2, "test2", "test2");

            Plugins.Add(new PostmanFeature());
*/
            Plugins.Add(new CorsFeature(
                allowOriginWhitelist: new[] { "http://localhost", "http://localhost:8080", "http://localhost:56500", "http://test.servicestack.net", "http://null.jsbin.com" },
                allowCredentials: true,
                allowedHeaders: "Content-Type, Allow, Authorization"));

            /*Plugins.Add(new RequestLogsFeature
            {
                RequestLogger = new RedisRequestLogger(container.Resolve<IRedisClientsManager>()),
            });*/

            Plugins.Add(new AuthFeature(() => new CustomUserSession(),
                new IAuthProvider[]
                {
                    new BasicAuthProvider(AppSettings),
                    new CredentialsAuthProvider(AppSettings),
                }));

            Plugins.Add(new Swagger2Feature());

            /*Plugins.Add(new AutoQueryFeature
            {
                MaxLimit = 100,
            });

            container.RegisterValidators(typeof(ThrowValidationValidator).Assembly);

            JavaGenerator.AddGsonImport = true;
            var nativeTypes = this.GetPlugin<NativeTypesFeature>();
            nativeTypes.MetadataTypesConfig.ExportTypes.Add(typeof(DayOfWeek));
            */


            /*this.RegisterRequestBinder<CustomRequestBinder>(
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
    }
}
