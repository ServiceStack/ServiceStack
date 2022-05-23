#define HTTP_LISTENER
using Funq;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.FluentValidation;
using ServiceStack.MiniProfiler;
using ServiceStack.MiniProfiler.Data;
using ServiceStack.OrmLite;
using ServiceStack.Razor;
using ServiceStack.Text;

#if HTTP_LISTENER
namespace ServiceStack.Auth.Tests
#else
namespace ServiceStack.AuthWeb.Tests
#endif
{
#if HTTP_LISTENER
    public class AppHost : AppHostHttpListenerBase
#else
    public class AppHost : AppHostBase
#endif
    {
        public AppHost()
            : base("Test Auth", typeof(AppHost).Assembly) { }

        public override void Configure(Container container)
        {
            Plugins.Add(new RequestLogsFeature {
                RequiredRoles = new[] { RoleNames.AllowAnon }
            });

            Plugins.Add(new RazorFormat());

            container.Register(new DataSource());

            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider) {
                    ConnectionFilter = x => new ProfiledDbConnection(x, Profiler.Current)
                });

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.CreateTableIfNotExists<Rockstar>();
                db.Insert(Rockstar.SeedData);
            }

            JsConfig.Init(new Text.Config {
                TextCase = TextCase.CamelCase
            });

            //Register Typed Config some services might need to access
            var appSettings = new AppSettings();

            //Register a external dependency-free 
            container.Register<ICacheClient>(new MemoryCacheClient());
            //Configure an alt. distributed persistent cache that survives AppDomain restarts. e.g Redis
            //container.Register<IRedisClientsManager>(c => new PooledRedisClientManager("localhost:6379"));

            //Enable Authentication an Registration
            ConfigureAuth(container);

            //Create your own custom User table
            using (var db = container.Resolve<IDbConnectionFactory>().Open())
                db.DropAndCreateTable<UserTable>();
        }

        private void ConfigureAuth(Container container)
        {
            //Enable and register existing services you want this host to make use of.
            //Look in Web.config for examples on how to configure your oauth providers, e.g. oauth.facebook.AppId, etc.
            var appSettings = new AppSettings();

            //Register all Authentication methods you want to enable for this web app.            
            Plugins.Add(new AuthFeature(
                () => new CustomUserSession(), //Use your own typed Custom UserSession type
                new IAuthProvider[] {
                    new CredentialsAuthProvider(),              //HTML Form post of UserName/Password credentials
                    new TwitterAuthProvider(appSettings),       //Sign-in with Twitter
                    new FacebookAuthProvider(appSettings),      //Sign-in with Facebook
                    new DigestAuthProvider(appSettings),        //Sign-in with Digest Auth
                    new BasicAuthProvider(),                    //Sign-in with Basic Auth
                    new GithubAuthProvider(appSettings),        //Sign-in with GitHub OAuth2 Provider
                }));

            //Provide service for new users to register so they can login with supplied credentials.
            Plugins.Add(new RegistrationFeature());

            //override the default registration validation with your own custom implementation
            container.RegisterAs<CustomRegistrationValidator, IValidator<Register>>();

            //Store User Data into the referenced SqlServer database
            container.Register<IAuthRepository>(c =>
                new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>())); //Use OrmLite DB Connection to persist the UserAuth and AuthProvider info

            var authRepo = (OrmLiteAuthRepository)container.Resolve<IAuthRepository>(); //If using and RDBMS to persist UserAuth, we must create required tables
            if (appSettings.Get("RecreateAuthTables", false))
                authRepo.DropAndReCreateTables(); //Drop and re-create all Auth and registration tables
            else
                authRepo.InitSchema();   //Create only the missing tables
        }
    }

    //Provide extra validation for the registration process
    public class CustomRegistrationValidator : RegistrationValidator
    {
        public CustomRegistrationValidator()
        {
            RuleSet(ApplyTo.Post, () =>
            {
                RuleFor(x => x.DisplayName).NotEmpty();
            });
        }
    }

    public class CustomUserSession : AuthUserSession
    {
        public override void OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens, System.Collections.Generic.Dictionary<string, string> authInfo)
        {
            "OnAuthenticated()".Print();
        }
    }

    public class DataSource
    {
        public string[] Items = new[] { "Eeny", "meeny", "miny", "moe" };
    }

    public class UserTable
    {
        public int Id { get; set; }
        public string CustomField { get; set; }
    }
}
