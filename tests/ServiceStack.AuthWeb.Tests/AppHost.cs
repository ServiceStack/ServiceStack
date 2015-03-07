//#define HTTP_LISTENER

using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Net;
using System.Threading;
using Funq;
using Raven.Client;
using Raven.Client.Document;
using ServiceStack.Admin;
using ServiceStack.Auth;
using ServiceStack.Authentication.OAuth2;
using ServiceStack.Authentication.OpenId;
using ServiceStack.Authentication.RavenDb;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation;
using ServiceStack.Logging;
using ServiceStack.MiniProfiler;
using ServiceStack.MiniProfiler.Data;
using ServiceStack.OrmLite;
using ServiceStack.Razor;
using ServiceStack.Redis;
using ServiceStack.Text;
using ServiceStack.Web;

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
        public static ILog Log = LogManager.GetLogger(typeof(AppHost));

        public AppHost()
            : base("Test Auth", typeof(AppHost).Assembly) { }

        public override void Configure(Container container)
        {
            Plugins.Add(new RazorFormat());
            Plugins.Add(new ServerEventsFeature());

            container.Register(new DataSource());

            var UsePostgreSql = false;
            if (UsePostgreSql)
            {
                container.Register<IDbConnectionFactory>(
                    new OrmLiteConnectionFactory(
                        "Server=localhost;Port=5432;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200", 
                        PostgreSqlDialect.Provider) {
                            ConnectionFilter = x => new ProfiledDbConnection(x, Profiler.Current)
                        });
            }
            else
            {
                container.Register<IDbConnectionFactory>(
                    new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider) {
                        ConnectionFilter = x => new ProfiledDbConnection(x, Profiler.Current)
                    });
            }

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<Rockstar>();
                db.Insert(Rockstar.SeedData);
            }

            JsConfig.EmitCamelCaseNames = true;

            //Register a external dependency-free 
            container.Register<ICacheClient>(new MemoryCacheClient());

            //Enable Authentication an Registration
            ConfigureAuth(container);

            //Create your own custom User table
            using (var db = container.Resolve<IDbConnectionFactory>().Open())
                db.DropAndCreateTable<UserTable>();

            SetConfig(new HostConfig {
                DebugMode = true,
                AddRedirectParamsToQueryString = true,
            });
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
                    //new AspNetWindowsAuthProvider(this) {
                    //    LoadUserAuthFilter = LoadUserAuthInfo,
                    //    AllowAllWindowsAuthUsers = true
                    //}, 
                    new CredentialsAuthProvider(),        //HTML Form post of UserName/Password credentials
                    new TwitterAuthProvider(appSettings),       //Sign-in with Twitter
                    new FacebookAuthProvider(appSettings),      //Sign-in with Facebook
                    new DigestAuthProvider(appSettings),        //Sign-in with Digest Auth
                    new BasicAuthProvider(),                    //Sign-in with Basic Auth
                    new GoogleOpenIdOAuthProvider(appSettings), //Sign-in with Google OpenId
                    new YahooOpenIdOAuthProvider(appSettings),  //Sign-in with Yahoo OpenId
                    new OpenIdOAuthProvider(appSettings),       //Sign-in with Custom OpenId
                    new GoogleOAuth2Provider(appSettings),      //Sign-in with Google OAuth2 Provider
                    new LinkedInOAuth2Provider(appSettings),    //Sign-in with LinkedIn OAuth2 Provider
                    new GithubAuthProvider(appSettings),        //Sign-in with GitHub OAuth Provider
                    new FourSquareOAuth2Provider(appSettings),  //Sign-in with FourSquare OAuth2 Provider
                    new YandexAuthProvider(appSettings),        //Sign-in with Yandex OAuth Provider        
                    new VkAuthProvider(appSettings),            //Sign-in with VK.com OAuth Provider 
                    new OdnoklassnikiAuthProvider(appSettings), //Sign-in with Odnoklassniki OAuth Provider 
                }));

#if HTTP_LISTENER
            //Required for DotNetOpenAuth in HttpListener 
            OpenIdOAuthProvider.OpenIdApplicationStore = new InMemoryOpenIdApplicationStore();
#endif

            //Provide service for new users to register so they can login with supplied credentials.
            Plugins.Add(new RegistrationFeature());

            //override the default registration validation with your own custom implementation
            //Plugins.Add(new CustomRegisterPlugin());

            var authRepo = CreateOrmLiteAuthRepo(container, appSettings);    //works with / or /basic
            //var authRepo = CreateRavenDbAuthRepo(container, appSettings);  //works with /basic
            //var authRepo = CreateRedisAuthRepo(container, appSettings);    //works with /basic
            //AuthProvider.ValidateUniqueUserNames = false;

            try
            {
                authRepo.CreateUserAuth(new CustomUserAuth
                {
                    Custom = "CustomUserAuth",
                    DisplayName = "Credentials",
                    FirstName = "First",
                    LastName = "Last",
                    FullName = "First Last",
                    Email = "demis.bellot@gmail.com",
                }, "test");

                authRepo.CreateUserAuth(new CustomUserAuth
                {
                    Custom = "CustomUserAuth",
                    DisplayName = "Credentials",
                    FirstName = "First",
                    LastName = "Last",
                    FullName = "First Last",
                    UserName = "mythz",
                }, "test");
            }
            catch (Exception) {}

            Plugins.Add(new RequestLogsFeature());
        }

        private static IUserAuthRepository CreateRavenDbAuthRepo(Container container, AppSettings appSettings)
        {
            container.Register<IDocumentStore>(c =>
                new DocumentStore { Url = "http://macbook:8080/" });

            var documentStore = container.Resolve<IDocumentStore>();
            documentStore.Initialize();

            container.Register<IAuthRepository>(c =>
                new RavenDbUserAuthRepository<CustomUserAuth, CustomUserAuthDetails>(c.Resolve<IDocumentStore>()));

            return (IUserAuthRepository)container.Resolve<IAuthRepository>();
        }

        private static IUserAuthRepository CreateRedisAuthRepo(Container container, AppSettings appSettings)
        {
            container.Register<IRedisClientsManager>(c =>
                new RedisManagerPool());

            //Configure an alt. distributed persistent cache that survives AppDomain restarts. e.g Redis
            container.Register(c => c.Resolve<IRedisClientsManager>().GetCacheClient());

            container.Register<IAuthRepository>(c =>
                new RedisAuthRepository(c.Resolve<IRedisClientsManager>()));

            var authRepo = (IUserAuthRepository)container.Resolve<IAuthRepository>();
            authRepo.InitSchema(); //unnecessary, but staying consistent
            
            return authRepo;
        }

        private static IUserAuthRepository CreateOrmLiteAuthRepo(Container container, AppSettings appSettings)
        {
            //Store User Data into the referenced SqlServer database
            container.Register<IAuthRepository>(c =>
                new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));
            
            //Use OrmLite DB Connection to persist the UserAuth and AuthProvider info
            var authRepo = (OrmLiteAuthRepository) container.Resolve<IAuthRepository>();
                //If using and RDBMS to persist UserAuth, we must create required tables
            if (appSettings.Get("RecreateAuthTables", false))
                authRepo.DropAndReCreateTables(); //Drop and re-create all Auth and registration tables
            else
                authRepo.InitSchema(); //Create only the missing tables

            return authRepo;
        }

        public void LoadUserAuthInfo(AuthUserSession userSession, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            if (userSession == null)
                return;

            try
            {
                using (PrincipalContext pc = new PrincipalContext(ContextType.Domain))
                {
                    var user = UserPrincipal.FindByIdentity(pc, userSession.UserAuthName);

                    tokens.DisplayName = user.DisplayName;
                    tokens.Email = user.EmailAddress;
                    tokens.FirstName = user.GivenName;
                    tokens.LastName = user.Surname;
                    tokens.FullName = (String.IsNullOrWhiteSpace(user.MiddleName))
                        ? "{0} {1}".Fmt(user.GivenName, user.Surname)
                        : "{0} {1} {2}".Fmt(user.GivenName, user.MiddleName, user.Surname);
                    tokens.PhoneNumber = user.VoiceTelephoneNumber;
                }
            }
            catch (MultipleMatchesException mmex)
            {
                Log.Error("Multiple windows user info for '{0}'".Fmt(userSession.UserAuthName), mmex);
            }
            catch (Exception ex)
            {
                Log.Error("Could not retrieve windows user info for '{0}'".Fmt(tokens.DisplayName), ex);
            }
        }
    }

    public class CustomUserAuth : UserAuth
    {
        public string Custom { get; set; }
    }

    public class CustomUserAuthDetails : UserAuthDetails
    {
        public string Custom { get; set; }
    }

    public class CustomCredentialsAuthProvider : CredentialsAuthProvider
    {
        public override bool TryAuthenticate(IServiceBase authService, string userName, string password)
        {
            if (password == "test")
                return true;

            throw HttpError.Unauthorized("Custom Error Message");
        }

        public override object Authenticate(IServiceBase authService, IAuthSession session, Authenticate request)
        {
            try
            {
                return base.Authenticate(authService, session, request);
            }
            catch (Exception ex)
            {
                return ex;
            }
        }
    }

    //Provide extra validation for the registration process
    public class CustomRegisterPlugin : IPlugin
    {
        public class CustomRegistrationValidator : RegistrationValidator
        {
            public CustomRegistrationValidator()
            {
                RuleSet(ApplyTo.Post, () =>
                {
                    RuleFor(x => x.UserName).Must(x => false)
                        .WithMessage("CustomRegistrationValidator is fired");
                });
            }
        }

        public void Register(IAppHost appHost)
        {
            appHost.RegisterAs<CustomRegistrationValidator, IValidator<Register>>();
        }
    }

    public class CustomUserSession : AuthUserSession
    {
        public string ProfileUrl64 { get; set; }

        public override void OnAuthenticated(IServiceBase authService, IAuthSession session, IAuthTokens tokens, Dictionary<string, string> authInfo)
        {
            base.OnAuthenticated(authService, session, tokens, authInfo);

            var jsv = authService.Request.Dto.Dump();
            "OnAuthenticated(): {0}".Print(jsv);

            this.ProfileUrl64 = session.GetProfileUrl();
        }

        public override void OnRegistered(IRequest httpReq, IAuthSession session, IServiceBase service)
        {
            "OnRegistered()".Print();
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

    [Route("/channels/{Channel}/chat")]
    public class PostChatToChannel : IReturn<ChatMessage>
    {
        public string From { get; set; }
        public string ToUserId { get; set; }
        public string Channel { get; set; }
        public string Message { get; set; }
        public string Selector { get; set; }
    }

    public class ChatMessage
    {
        public long Id { get; set; }
        public string FromUserId { get; set; }
        public string FromName { get; set; }
        public string DisplayName { get; set; }
        public string Message { get; set; }
        public string UserAuthId { get; set; }
        public bool Private { get; set; }
    }

    [Route("/channels/{Channel}/raw")]
    public class PostRawToChannel : IReturnVoid
    {
        public string From { get; set; }
        public string ToUserId { get; set; }
        public string Channel { get; set; }
        public string Message { get; set; }
        public string Selector { get; set; }
    }

    public class ServerEventsService : Service
    {
        private static long msgId;

        public IServerEvents ServerEvents { get; set; }

        public object Any(PostChatToChannel request)
        {
            var sub = ServerEvents.GetSubscriptionInfo(request.From);
            if (sub == null)
                throw HttpError.NotFound("Subscription {0} does not exist".Fmt(request.From));

            var msg = new ChatMessage
            {
                Id = Interlocked.Increment(ref msgId),
                FromUserId = sub.UserId,
                FromName = sub.DisplayName,
                Message = request.Message,
            };

            if (request.ToUserId != null)
            {
                msg.Private = true;
                ServerEvents.NotifyUserId(request.ToUserId, request.Selector, msg);
                var toSubs = ServerEvents.GetSubscriptionInfosByUserId(request.ToUserId);
                foreach (var toSub in toSubs)
                {
                    msg.Message = "@{0}: {1}".Fmt(toSub.DisplayName, msg.Message);
                    ServerEvents.NotifySubscription(request.From, request.Selector, msg);
                }
            }
            else
            {
                ServerEvents.NotifyChannel(request.Channel, request.Selector, msg);
            }

            return msg;
        }

        public void Any(PostRawToChannel request)
        {
            var sub = ServerEvents.GetSubscriptionInfo(request.From);
            if (sub == null)
                throw HttpError.NotFound("Subscription {0} does not exist".Fmt(request.From));

            if (request.ToUserId != null)
            {
                ServerEvents.NotifyUserId(request.ToUserId, request.Selector, request.Message);
            }
            else
            {
                ServerEvents.NotifyChannel(request.Channel, request.Selector, request.Message);
            }
        }
    }

}
