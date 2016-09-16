using System;
using System.Linq;
using System.Runtime.Serialization;
using Funq;
using NUnit.Framework;
using ServiceStack.Admin;
using ServiceStack.Auth;
using ServiceStack.Authentication.OpenId;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.Host;
using ServiceStack.Messaging;
using ServiceStack.Messaging.Redis;
using ServiceStack.MiniProfiler;
using ServiceStack.MiniProfiler.Data;
using ServiceStack.OrmLite;
using ServiceStack.ProtoBuf;
using ServiceStack.Redis;
using ServiceStack.Api.Swagger;
using ServiceStack.Common.Tests;
using ServiceStack.DataAnnotations;
using ServiceStack.Shared.Tests;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.Web;
using ServiceStack.WebHost.IntegrationTests.Services;
using ServiceStack.WebHost.IntegrationTests.Tests;

namespace ServiceStack.WebHost.IntegrationTests
{
    public class Global : System.Web.HttpApplication
    {
        private const bool StartMqHost = false;

        public class AppHost
            : AppHostBase
        {
            public AppHost()
                : base("ServiceStack WebHost IntegrationTests", typeof(Reverse).Assembly)
            {
                //typeof(Authenticate)
                //    .AddAttributes(new ExcludeAttribute(Feature.Metadata));

                foreach (var pi in typeof(Authenticate).GetPublicProperties())
                {
                    if (pi.Name != "provider" && pi.Name != "UserName" && pi.Name != "Password")
                    {
                        pi.AddAttributes(new IgnoreDataMemberAttribute());
                    }
                }
            }

            public override void Configure(Container container)
            {
                IocShared.Configure(this);

                JsConfig.EmitCamelCaseNames = true;

                this.PreRequestFilters.Add((req, res) =>
                {
                    req.Items["_DataSetAtPreRequestFilters"] = true;
                });

                this.GlobalRequestFilters.Add((req, res, dto) =>
                {
                    req.Items["_DataSetAtRequestFilters"] = true;

                    var requestFilter = dto as RequestFilter;
                    if (requestFilter != null)
                    {
                        res.StatusCode = requestFilter.StatusCode;
                        if (!requestFilter.HeaderName.IsNullOrEmpty())
                        {
                            res.AddHeader(requestFilter.HeaderName, requestFilter.HeaderValue);
                        }
                        res.Close();
                    }

                    var secureRequests = dto as IRequiresSession;
                    if (secureRequests != null)
                    {
                        res.ReturnAuthRequired();
                    }
                });

                this.Container.Register<IDbConnectionFactory>(c =>
                    new OrmLiteConnectionFactory(
                        "~/App_Data/db.sqlite".MapHostAbsolutePath(),
                        SqliteDialect.Provider)
                    {
                        ConnectionFilter = x => new ProfiledDbConnection(x, Profiler.Current)
                    });

                this.Container.Register<ICacheClient>(new MemoryCacheClient());
                //this.Container.Register<ICacheClient>(new BasicRedisClientManager());

                ConfigureAuth(container);

                //this.Container.Register<ISessionFactory>(
                //    c => new SessionFactory(c.Resolve<ICacheClient>()));

                var dbFactory = this.Container.Resolve<IDbConnectionFactory>();

                using (var db = dbFactory.Open())
                    db.DropAndCreateTable<Movie>();

                ModelConfig<Movie>.Id(x => x.Title);
                Routes
                    .Add<Movies>("/custom-movies", "GET, OPTIONS")
                    .Add<Movies>("/custom-movies/genres/{Genre}")
                    .Add<Movie>("/custom-movies", "POST,PUT")
                    .Add<Movie>("/custom-movies/{Id}")
                    .Add<MqHostStats>("/mqstats");


                var resetMovies = this.Container.Resolve<ResetMoviesService>();
                resetMovies.Post(null);

                container.Register<IRedisClientsManager>(c => new RedisManagerPool());

                Plugins.Add(new ValidationFeature());
                Plugins.Add(new SessionFeature());
                Plugins.Add(new ProtoBufFormat());
                Plugins.Add(new RequestLogsFeature
                {
                    //RequestLogger = new RedisRequestLogger(container.Resolve<IRedisClientsManager>())
                    RequestLogger = new CsvRequestLogger(),
                });
                Plugins.Add(new SwaggerFeature
                {
                    //UseBootstrapTheme = true
                    OperationFilter = x => x.Consumes = x.Produces = new[] { MimeTypes.Json, MimeTypes.Xml }.ToList(),
                    RouteSummary =
                    {
                        { "/swaggerexamples", "Swagger Examples Summary" }
                    }
                });
                Plugins.Add(new PostmanFeature());
                Plugins.Add(new CorsFeature());
                Plugins.Add(new AutoQueryFeature { MaxLimit = 100 });
                Plugins.Add(new AdminFeature());

                container.RegisterValidators(typeof(CustomersValidator).Assembly);

                typeof(ResponseStatus)
                    .AddAttributes(new ServiceStack.DataAnnotations.DescriptionAttribute("This is the Response Status!"));

                typeof(ResponseStatus)
                   .GetProperty("Message")
                   .AddAttributes(new ServiceStack.DataAnnotations.DescriptionAttribute("A human friendly error message"));

                //var onlyEnableFeatures = Feature.All.Remove(Feature.Jsv | Feature.Soap);
                SetConfig(new HostConfig
                {
                    AdminAuthSecret = AuthTestsBase.AuthSecret,
                    ApiVersion = "0.2.0",
                    //EnableFeatures = onlyEnableFeatures,
                    DebugMode = true, //Show StackTraces for easier debugging
                });

                if (StartMqHost)
                {
                    var redisManager = new BasicRedisClientManager();
                    var mqHost = new RedisMqServer(redisManager);
                    mqHost.RegisterHandler<Reverse>(ExecuteMessage);
                    mqHost.Start();
                    this.Container.Register((IMessageService)mqHost);
                }
            }

            //Configure ServiceStack Authentication and CustomUserSession
            private void ConfigureAuth(Funq.Container container)
            {
                Routes
                    .Add<Register>("/register");

                var appSettings = new AppSettings();

                Plugins.Add(new AuthFeature(() => new CustomUserSession(),
                    new IAuthProvider[] {
                        new CredentialsAuthProvider(appSettings),
                        new FacebookAuthProvider(appSettings),
                        new TwitterAuthProvider(appSettings),
                        new GoogleOpenIdOAuthProvider(appSettings),
                        new OpenIdOAuthProvider(appSettings),
                        new DigestAuthProvider(appSettings),
                        new BasicAuthProvider(appSettings),
                    }));

                Plugins.Add(new RegistrationFeature());

                container.Register<IAuthRepository>(c =>
                    new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));

                var authRepo = (OrmLiteAuthRepository)container.Resolve<IAuthRepository>();
                if (new AppSettings().Get("RecreateTables", true))
                    authRepo.DropAndReCreateTables();
                else
                    authRepo.InitSchema();
            }

            public override object OnPreExecuteServiceFilter(IService service, object request, IRequest httpReq, IResponse httpRes)
            {
                if (service is IocScopeService)
                    service.InjectRequestIntoDependencies(httpReq);
                return request;
            }
        }

        protected void Application_Start(object sender, EventArgs e)
        {
            var appHost = new AppHost();
            appHost.Init();
        }

        protected void Application_BeginRequest(object src, EventArgs e)
        {
            if (Request.IsLocal)
                Profiler.Start();
        }

        protected void Application_EndRequest(object src, EventArgs e)
        {
            Profiler.Stop();

            var mqHost = HostContext.TryResolve<IMessageService>();
            if (mqHost != null)
                mqHost.Start();
        }
    }
}