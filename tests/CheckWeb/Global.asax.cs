using System;
using System.Collections.Generic;
using System.Linq;
using Check.ServiceInterface;
using Check.ServiceModel;
using Check.ServiceModel.Types;
using Funq;
using ServiceStack;
using ServiceStack.Admin;
using ServiceStack.Api.Swagger;
using ServiceStack.Auth;
using ServiceStack.Data;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.MiniProfiler;
using ServiceStack.MiniProfiler.Data;
using ServiceStack.OrmLite;
using ServiceStack.Razor;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.VirtualPath;

namespace CheckWeb
{
    public class AppHost : AppHostBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppHost"/> class.
        /// </summary>
        public AppHost()
            : base("CheckWeb", typeof(ErrorsService).Assembly) {}

        /// <summary>
        /// Configure the Web Application host.
        /// </summary>
        /// <param name="container">The container.</param>
        public override void Configure(Container container)
        {
            var nativeTypes = this.GetPlugin<NativeTypesFeature>();
            nativeTypes.MetadataTypesConfig.ExportTypes.Add(typeof(DayOfWeek));
            nativeTypes.MetadataTypesConfig.IgnoreTypes.Add(typeof(IgnoreInMetadataConfig));
            nativeTypes.InitializeCollectionsForType = NativeTypesFeature.DontInitializeAutoQueryCollections;
            //nativeTypes.MetadataTypesConfig.GlobalNamespace = "Check.ServiceInterface";

            // Change ServiceStack configuration
            this.SetConfig(new HostConfig
            {
                //UseHttpsLinks = true,
                AppendUtf8CharsetOnContentTypes = new HashSet<string> { MimeTypes.Html },
                //AllowJsConfig = false,

                // Set to return JSON if no request content type is defined
                // e.g. text/html or application/json
                //DefaultContentType = MimeTypes.Json,
#if !DEBUG
                // Show StackTraces in service responses during development
                DebugMode = true,
#endif
                // Disable SOAP endpoints
                //EnableFeatures = Feature.All.Remove(Feature.Soap)
                //EnableFeatures = Feature.All.Remove(Feature.Metadata)
            });

            container.Register<IServiceClient>(c =>
                new JsonServiceClient("http://localhost:55799/") {
                    CaptureSynchronizationContext = true,
                });

            Plugins.Add(new AutoQueryFeature { MaxLimit = 100 });

            Plugins.Add(new AutoQueryDataFeature()
                .AddDataSource(ctx => ctx.MemorySource(GetRockstars())));

            Plugins.Add(new AdminFeature());

            Plugins.Add(new PostmanFeature());
            Plugins.Add(new CorsFeature(allowedMethods: "GET, POST, PUT, DELETE, PATCH, OPTIONS"));

            Plugins.Add(new RequestLogsFeature {
                RequestLogger = new CsvRequestLogger(),
            });

            Plugins.Add(new DynamicallyRegisteredPlugin());

            container.Register<IDbConnectionFactory>(
                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));

            using (var db = container.Resolve<IDbConnectionFactory>().Open())
            {
                db.DropAndCreateTable<Rockstar>();
                db.InsertAll(GetRockstars());
            }

            var dbFactory = (OrmLiteConnectionFactory)container.Resolve<IDbConnectionFactory>();

            dbFactory.RegisterConnection("SqlServer", 
                new OrmLiteConnectionFactory(
                    "Server=localhost;Database=test;User Id=test;Password=test;",
                    SqlServerDialect.Provider) {
                        ConnectionFilter = x => new ProfiledDbConnection(x, Profiler.Current)
                    });

            dbFactory.RegisterConnection("pgsql",
                new OrmLiteConnectionFactory(
                    "Server=localhost;Port=5432;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200",
                    PostgreSqlDialect.Provider));

            using (var db = dbFactory.OpenDbConnection("pgsql"))
            {
                db.DropAndCreateTable<Rockstar>();
                db.DropAndCreateTable<PgRockstar>();

                db.Insert(new Rockstar { Id = 1, FirstName = "PostgreSQL", LastName = "Connection", Age = 1 });
                db.Insert(new PgRockstar { Id = 1, FirstName = "PostgreSQL", LastName = "Named Connection", Age = 1 });
            }

            this.GlobalHtmlErrorHttpHandler = new RazorHandler("GlobalErrorHandler.cshtml");

            // Configure JSON serialization properties.
            this.ConfigureSerialization(container);

            // Configure ServiceStack database connections.
            this.ConfigureDataConnection(container);

            // Configure ServiceStack Authentication plugin.
            this.ConfigureAuth(container);

            // Configure ServiceStack Fluent Validation plugin.
            this.ConfigureValidation(container);

            // Configure ServiceStack Razor views.
            this.ConfigureView(container);
        }

        public static Rockstar[] GetRockstars()
        {
            return new[]
            {
                new Rockstar {Id = 1, FirstName = "Jimi", LastName = "Hendrix", Age = 27},
                new Rockstar {Id = 2, FirstName = "Jim", LastName = "Morrison", Age = 27},
                new Rockstar {Id = 3, FirstName = "Kurt", LastName = "Cobain", Age = 27},
                new Rockstar {Id = 4, FirstName = "Elvis", LastName = "Presley", Age = 42},
                new Rockstar {Id = 5, FirstName = "David", LastName = "Grohl", Age = 44},
                new Rockstar {Id = 6, FirstName = "Eddie", LastName = "Vedder", Age = 48},
                new Rockstar {Id = 7, FirstName = "Michael", LastName = "Jackson", Age = 50},
            };
        }

        /// <summary>
        /// Configure JSON serialization properties.
        /// </summary>
        /// <param name="container">The container.</param>
        private void ConfigureSerialization(Container container)
        {
            // Set JSON web services to return idiomatic JSON camelCase properties
            //JsConfig.EmitCamelCaseNames = true;
            //JsConfig.EmitLowercaseUnderscoreNames = true;

            // Set JSON web services to return ISO8601 date format
            JsConfig.DateHandler = DateHandler.ISO8601;

            // Exclude type info during serialization as an effect of IoC
            JsConfig.ExcludeTypeInfo = true;
        }

        /// <summary>
        /// // Configure ServiceStack database connections.
        /// </summary>
        /// <param name="container">The container.</param>
        private void ConfigureDataConnection(Container container)
        {
            // ...
        }

        /// <summary>
        /// Configure ServiceStack Authentication plugin.
        /// </summary>
        /// <param name="container">The container.</param>
        private void ConfigureAuth(Container container)
        {
            Plugins.Add(new AuthFeature(() => new AuthUserSession(), 
                new IAuthProvider[]
                {
                    new BasicAuthProvider(AppSettings), 
                    new ApiKeyAuthProvider(AppSettings), 
                })
            {
                ServiceRoutes = new Dictionary<Type, string[]> {
                  { typeof(AuthenticateService), new[] { "/api/auth", "/api/auth/{provider}" } },
                }
            });

            var authRepo = new OrmLiteAuthRepository(container.Resolve<IDbConnectionFactory>());
            container.Register<IAuthRepository>(c => authRepo);
            authRepo.InitSchema();
        }

        /// <summary>
        /// Configure ServiceStack Fluent Validation plugin.
        /// </summary>
        /// <param name="container">The container.</param>
        private void ConfigureValidation(Container container)
        {
            // Provide fluent validation functionality for web services
            Plugins.Add(new ValidationFeature());

            container.RegisterValidators(typeof(AppHost).Assembly);
        }

        /// <summary>
        /// Configure ServiceStack Razor views.
        /// </summary>
        /// <param name="container">The container.</param>
        private void ConfigureView(Container container)
        {
            // Enable ServiceStack Razor
            var razor = new RazorFormat();
            razor.Deny.RemoveAt(0);
            Plugins.Add(razor);

            // Enable support for Swagger API browser
            Plugins.Add(new SwaggerFeature
            {
                UseBootstrapTheme = true, 
                LogoUrl = "//lh6.googleusercontent.com/-lh7Gk4ZoVAM/AAAAAAAAAAI/AAAAAAAAAAA/_0CgCb4s1e0/s32-c/photo.jpg"
            });
            //Plugins.Add(new CorsFeature()); // Uncomment if the services to be available from external sites
        }

        public override List<IVirtualPathProvider> GetVirtualFileSources()
        {
            var existingProviders = base.GetVirtualFileSources();
            var memFs = new InMemoryVirtualPathProvider(this);

            //Get FileSystem Provider
            var fs = existingProviders.First(x => x is FileSystemVirtualPathProvider);

            //Process all .html files:
            foreach (var file in fs.GetAllMatchingFiles("*.html"))
            {
                var contents = Minifiers.HtmlAdvanced.Compress(file.ReadAllText());
                memFs.WriteFile(file.VirtualPath, contents);
            }

            //Process all .css files:
            foreach (var file in fs.GetAllMatchingFiles("*.css")
                .Where(file => !file.VirtualPath.EndsWith(".min.css")))
            {
                var contents = Minifiers.Css.Compress(file.ReadAllText());
                memFs.WriteFile(file.VirtualPath, contents);
            }

            //Process all .js files
            foreach (var file in fs.GetAllMatchingFiles("*.js")
                .Where(file => !file.VirtualPath.EndsWith(".min.js")))
            {
                try
                {
                    var js = file.ReadAllText();
                    var contents = Minifiers.JavaScript.Compress(js);
                    memFs.WriteFile(file.VirtualPath, contents);
                }
                catch (Exception ex)
                {
                    //Report any errors in StartUpErrors collection on ?debug=requestinfo
                    base.OnStartupException(new Exception("JSMin Error in {0}: {1}".Fmt(file.VirtualPath, ex.Message)));
                }
            }

            //Give new Memory FS highest priority
            existingProviders.Insert(0, memFs);
            return existingProviders;
        }
    }

    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            new AppHost().Init();
        }

        protected void Application_BeginRequest(object src, EventArgs e)
        {
            if (Request.IsLocal)
                Profiler.Start();
        }

        protected void Application_EndRequest(object src, EventArgs e)
        {
            Profiler.Stop();
        }
    }
}