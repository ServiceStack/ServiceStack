using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Check.ServiceInterface;
using Check.ServiceModel;
using Check.ServiceModel.Operations;
using Check.ServiceModel.Types;
using Funq;
using ServiceStack;
using ServiceStack.Api.OpenApi;
using ServiceStack.Api.OpenApi.Specification;
using ServiceStack.Auth;
using ServiceStack.Configuration;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.MiniProfiler;
using ServiceStack.ProtoBuf;
using ServiceStack.Razor;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.DataAnnotations;
using ServiceStack.Redis;
using ServiceStack.Logging;

namespace CheckWeb
{
    public class AppHost : AppHostBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppHost"/> class.
        /// </summary>
        public AppHost()
            : base("CheckWeb", typeof(ErrorsService).Assembly, typeof(HtmlServices).Assembly) { }

        // public override void HttpCookieFilter(HttpCookie cookie)
        // {
        //     cookie.SameSite = SameSiteMode.None;
        // }

        /// <summary>
        /// Configure the Web Application host.
        /// </summary>
        /// <param name="container">The container.</param>
        public override void Configure(Container container)
        {
//            EnableBuffering();

            this.CustomErrorHttpHandlers[HttpStatusCode.NotFound] = new RazorHandler("/Views/TestErrorNotFound");
            
            // Change ServiceStack configuration
            SetConfig(new HostConfig
            {
                DebugMode = true,
                //UseHttpsLinks = true,
                AppendUtf8CharsetOnContentTypes = { MimeTypes.Html },
                CompressFilesWithExtensions = { "js", "css" },
                UseCamelCase = true,
                AdminAuthSecret = "secretz",
                //HandlerFactoryPath = "CheckWeb", //when hosted on IIS
                //AllowJsConfig = false,

                // Set to return JSON if no request content type is defined
                // e.g. text/html or application/json
                //DefaultContentType = MimeTypes.Json,
                // Disable SOAP endpoints
                //EnableFeatures = Feature.All.Remove(Feature.Soap)
                //EnableFeatures = Feature.All.Remove(Feature.Metadata)
            });

            container.Register<IServiceClient>(c =>
                new JsonServiceClient("http://localhost:55799/"));

            Plugins.Add(new SharpPagesFeature
            {
                MetadataDebugAdminRole = RoleNames.AllowAnyUser, 
                ScriptAdminRole = RoleNames.AllowAnon,
            });

            //ProxyFeatureTests
            Plugins.Add(new ProxyFeature(
                matchingRequests: req => req.PathInfo.StartsWith("/proxy/test"),
                resolveUrl: req => "http://test.servicestack.net".CombineWith(req.RawUrl.Replace("/test", "/"))));

            Plugins.Add(new ProxyFeature(
                matchingRequests: req => req.PathInfo.StartsWith("/techstacks"),
                resolveUrl: req => "http://techstacks.io".CombineWith(req.RawUrl.Replace("/techstacks", "/"))));

            Plugins.Add(new AutoQueryFeature { MaxLimit = 100 });

            Plugins.Add(new AutoQueryDataFeature()
                .AddDataSource(ctx => ctx.MemorySource(GetRockstars())));

            //Plugins.Add(new AdminFeature());

            Plugins.Add(new PostmanFeature());
            Plugins.Add(new CorsFeature(
                allowOriginWhitelist: new[] { "http://localhost", "http://localhost:8080", "http://localhost:56500", "http://test.servicestack.net", "http://null.jsbin.com" },
                allowCredentials: true,
                allowedHeaders: "Content-Type, Allow, Authorization, X-Args"));

            Plugins.Add(new ServerEventsFeature
            {
                LimitToAuthenticatedUsers = true
            });

            GlobalRequestFilters.Add((req, res, dto) =>
            {
                if (dto is AlwaysThrowsGlobalFilter)
                    throw new Exception(dto.GetType().Name);
            });

            Plugins.Add(new RequestLogsFeature
            {
                RequestLogger = new CsvRequestLogger(),
                EnableResponseTracking = true
            });

            Plugins.Add(new DynamicallyRegisteredPlugin());

            var nativeTypes = GetPlugin<NativeTypesFeature>();
            nativeTypes.MetadataTypesConfig.ExportAttributes.Add(typeof(DisplayAttribute));
            nativeTypes.MetadataTypesConfig.ExportAttributes.Add(typeof(DisplayColumnAttribute));
            nativeTypes.MetadataTypesConfig.ExportAttributes.Add(typeof(DisplayFormatAttribute));
            nativeTypes.MetadataTypesConfig.ExportAttributes.Add(typeof(DataTypeAttribute));
            nativeTypes.MetadataTypesConfig.ExportAttributes.Add(typeof(EditableAttribute));
            nativeTypes.MetadataTypesConfig.ExportAttributes.Add(typeof(ServiceStack.DataAnnotations.PrimaryKeyAttribute));
            nativeTypes.MetadataTypesConfig.ExportAttributes.Add(typeof(ServiceStack.DataAnnotations.AutoIncrementAttribute));
            nativeTypes.MetadataTypesConfig.ExportAttributes.Add(typeof(ServiceStack.DataAnnotations.AutoIdAttribute));
            nativeTypes.MetadataTypesConfig.ExportAttributes.Add(typeof(System.ComponentModel.BindableAttribute));
            nativeTypes.MetadataTypesConfig.ExportAttributes.Add(typeof(AssociationAttribute));

            nativeTypes.ExportAttribute<DisplayAttribute>(x =>
            {
                var metadata = nativeTypes.GetGenerator().ToMetadataAttribute(x);
                try
                {
                    var attr = (DisplayAttribute)x;
                    if (attr.GetAutoGenerateField() == null || (attr.GetAutoGenerateField().HasValue && !attr.GetAutoGenerateField().Value))
                        metadata.Args.Add(new MetadataPropertyType {
                            Name = nameof(DisplayAttribute.AutoGenerateField), 
                            Namespace = "System", 
                            Type = nameof(Boolean), 
                            Value = "false"
                        });
                    return metadata;
                }
                catch (Exception ex)
                {
                    throw;
                }
            });            
            

//            container.Register<IDbConnectionFactory>(
//                new OrmLiteConnectionFactory(":memory:", SqliteDialect.Provider));
//            //container.Register<IDbConnectionFactory>(
//            //    new OrmLiteConnectionFactory("Server=localhost;Database=test;User Id=test;Password=test;", SqlServerDialect.Provider));
//
//            using (var db = container.Resolve<IDbConnectionFactory>().Open())
//            {
//                db.DropAndCreateTable<Rockstar>();
//                db.InsertAll(GetRockstars());
//
//                db.DropAndCreateTable<AllTypes>();
//                db.Insert(new AllTypes
//                {
//                    Id = 1,
//                    Int = 2,
//                    Long = 3,
//                    Float = 1.1f,
//                    Double = 2.2,
//                    Decimal = 3.3m,
//                    DateTime = DateTime.Now,
//                    Guid = Guid.NewGuid(),
//                    TimeSpan = TimeSpan.FromMilliseconds(1),
//                    String = "String"
//                });
//            }
//
//            Plugins.Add(new MiniProfilerFeature());
//
//            var dbFactory = (OrmLiteConnectionFactory)container.Resolve<IDbConnectionFactory>();
//            dbFactory.RegisterConnection("SqlServer",
//                new OrmLiteConnectionFactory(
//                    "Server=localhost;Database=test;User Id=test;Password=test;",
//                    SqlServerDialect.Provider)
//                {
//                    ConnectionFilter = x => new ProfiledDbConnection(x, Profiler.Current)
//                });
//
//            dbFactory.RegisterConnection("pgsql",
//                new OrmLiteConnectionFactory(
//                    Environment.GetEnvironmentVariable("PGSQL_CONNECTION") ?? 
//                    "Server=localhost;Port=5432;User Id=test;Password=test;Database=test;Pooling=true;MinPoolSize=0;MaxPoolSize=200",
//                    PostgreSqlDialect.Provider));
//
//            using (var db = dbFactory.OpenDbConnection("pgsql"))
//            {
//                db.DropAndCreateTable<Rockstar>();
//                db.DropAndCreateTable<PgRockstar>();
//
//                db.Insert(new Rockstar { Id = 1, FirstName = "PostgreSQL", LastName = "Connection", Age = 1 });
//                db.Insert(new PgRockstar { Id = 1, FirstName = "PostgreSQL", LastName = "Named Connection", Age = 1 });
//            }

            //this.GlobalHtmlErrorHttpHandler = new RazorHandler("GlobalErrorHandler.cshtml");

            // Configure JSON serialization properties.
            this.ConfigureSerialization(container);

            // Configure ServiceStack database connections.
            this.ConfigureDataConnection(container);

            // Configure ServiceStack Authentication plugin.
            this.ConfigureAuth(container);

            // Configure ServiceStack Razor views.
            this.ConfigureView(container);

            this.StartUpErrors.Add(new ResponseStatus("Mock", "Startup Error"));

            //PreRequestFilters.Add((req, res) =>
            //{
            //    if (req.PathInfo.StartsWith("/metadata") || req.PathInfo.StartsWith("/swagger-ui"))
            //    {
            //        var session = req.GetSession();
            //        if (!session.IsAuthenticated)
            //        {
            //            res.StatusCode = (int)HttpStatusCode.Unauthorized;
            //            res.EndRequest();
            //        }
            //    }
            //});
            
            Plugins.Add(new ProtoBufFormat());

            AfterPluginsLoaded.Add(appHost =>
            {
                // Configure ServiceStack Fluent Validation plugin.
                this.ConfigureValidation(container);
            });
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
            // Set JSON web services to return ISO8601 date format
            // Exclude type info during serialization as an effect of IoC
            JsConfig.Init(new ServiceStack.Text.Config {
                DateHandler = DateHandler.ISO8601,
                ExcludeTypeInfo = true,
            });
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
                    new CredentialsAuthProvider(AppSettings),
                    new JwtAuthProvider(AppSettings)
                    {
                        AuthKey = Convert.FromBase64String("3n/aJNQHPx0cLu/2dN3jWf0GSYL35QlMqgz+LH3hUyA="),
                        RequireSecureConnection = false,
                    },
//                    new ApiKeyAuthProvider(AppSettings),
                    new BasicAuthProvider(AppSettings),
                }));

            Plugins.Add(new RegistrationFeature());

//            var authRepo = new OrmLiteAuthRepository(container.Resolve<IDbConnectionFactory>());
//            container.Register<IAuthRepository>(c => authRepo);
//            authRepo.InitSchema();
//
//            authRepo.CreateUserAuth(new UserAuth
//            {
//                UserName = "test",
//                DisplayName = "Credentials",
//                FirstName = "First",
//                LastName = "Last",
//                FullName = "First Last",
//            }, "test");
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
            container.RegisterValidators(typeof(ThrowValidationValidator).Assembly);
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

            container.Register<IRedisClientsManager>(c => new RedisManagerPool());

            Plugins.Add(new OpenApiFeature
            {
                ApiDeclarationFilter = api =>
                {
                    foreach (var path in new[] { api.Paths["/auth"], api.Paths["/auth/{provider}"] })
                    {
                        path.Get = path.Put = path.Delete = null;
                    }
                },
                OperationFilter = (verb, op) => {
                    if (op.RequestType == nameof(SwaggerRangeTest))
                    {
                        var intRange = op.Parameters.FirstOrDefault(p => p.Name == nameof(SwaggerRangeTest.IntRange));
                        intRange.Minimum = 1;
                        intRange.Maximum = 2;

                        var dobleRange = op.Parameters.FirstOrDefault(p => p.Name == nameof(SwaggerRangeTest.DoubleRange));
                        dobleRange.Minimum = 1.1;
                        dobleRange.Maximum = 2.2;
                    }
                },
                Tags =
                {
                    new OpenApiTag
                    {
                        Name = "TheTag",
                        Description = "TheTag Description",
                        ExternalDocs = new OpenApiExternalDocumentation
                        {
                            Description = "Link to External Docs Desc",
                            Url = "http://example.org/docs/path",
                        }
                    }
                }
            });

            // Enable support for Swagger API browser
            //Plugins.Add(new SwaggerFeature
            //{
            //    UseBootstrapTheme = true,
            //    LogoUrl = "//lh6.googleusercontent.com/-lh7Gk4ZoVAM/AAAAAAAAAAI/AAAAAAAAAAA/_0CgCb4s1e0/s32-c/photo.jpg"
            //});
            //Plugins.Add(new CorsFeature()); // Uncomment if the services to be available from external sites
        }

        public void EnableBuffering()
        {
            PreRequestFilters.Add((req, res) =>
            {
                req.UseBufferedStream = true;
                res.UseBufferedStream = true;
            });
        }

        public override List<IVirtualPathProvider> GetVirtualFileSources()
        {
            var existingProviders = base.GetVirtualFileSources();
            //return existingProviders;

            var memFs = new MemoryVirtualFiles();

            //Get FileSystem Provider
            var fs = existingProviders.First(x => x is FileSystemVirtualFiles);

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

    [Route("/query/alltypes")]
    public class QueryAllTypes : QueryDb<AllTypes> { }

    [Route("/test/html")]
    public class TestHtml : IReturn<TestHtml>
    {
        public string Name { get; set; }
    }

    [Route("/test/html2")]
    public class TestHtml2
    {
        public string Name { get; set; }
    }

    [HtmlOnly]
    [CacheResponse(Duration = 3600)]
    public class HtmlServices : Service
    {
        public object Any(TestHtml request) => request;

        public object Any(TestHtml2 request) => new HttpResult(new TestHtml { Name = request.Name })
        {
            View = nameof(TestHtml)
        };
    }

    [Route("/views/request")]
    public class ViewRequest : IReturn<ViewResponse>
    {
        public string Name { get; set; }
    }

    public class ViewResponse
    {
        public string Result { get; set; }
    }
    
    public class ViewServices : Service
    {
        public object Get(ViewRequest request)
        {
            var result = Gateway.Send(new TestHtml());
            return new ViewResponse { Result = request.Name };
        }

        public object Get(ViewRequest[] requests)
        {
            return requests.Map(x => new ViewResponse {Result = x.Name}).ToArray();
        }
    }

    [Route("/index")]
    public class IndexPage
    {
        public string PathInfo { get; set; }
    }

    [Route("/return/text")]
    public class ReturnText
    {
        public string Text { get; set; }
    }

    [Route("/swagger/model")]
    public class SwaggerModel : IReturn<SwaggerModel>
    {
        public int Int { get; set; }
        public string String { get; set; }
        public DateTime DateTime { get; set; }
        public DateTimeOffset DateTimeOffset { get; set; }
        public TimeSpan TimeSpan { get; set; }
    }

    [Route("/async/redis")]
    [Route("/async/redis/{Incr}")]
    public class AsyncRedis : IReturn<IdResponse>
    {
        public uint Incr { get; set; }
    }

    public class MyServices : Service
    {
        //Return default.html for unmatched requests so routing is handled on client
        public object Any(IndexPage request) =>
            new HttpResult(VirtualFileSources.GetFile("default.html"));

        [AddHeader(ContentType = MimeTypes.PlainText)]
        public object Any(ReturnText request) => request.Text;

        public object Any(SwaggerModel request) => request;

        public async Task<object> Any(AsyncRedis request)
        {
            var redis = await GetRedisAsync();
            await redis.IncrementAsync(nameof(AsyncRedis), request.Incr);
            
            var response = new IdResponse {
                Id = (await redis.GetAsync<int>(nameof(AsyncRedis))).ToString()
            };
            return response;
        }
    }

    [Route("/plain-dto")]
    public class PlainDto : IReturn<PlainDto>
    {
        public string Name { get; set; }
    }

    [Route("/httpresult-dto")]
    public class HttpResultDto : IReturn<HttpResultDto>
    {
        public string Name { get; set; }
    }

    public class HttpResultServices : Service
    {
        public object Any(PlainDto request) => request;

        public object Any(HttpResultDto request) => new HttpResult(request, HttpStatusCode.Created);
    }

    [Route("/restrict/mq")]
    [Restrict(RequestAttributes.MessageQueue)]
    public class TestMqRestriction : IReturn<TestMqRestriction>
    {
        public string Name { get; set; }
    }

    public class TestRestrictionsService : Service
    {
        public object Any(TestMqRestriction request) => request;
    }

    [Route("/set-cache")]
    public class SetCache : IReturn<SetCache>
    {
        public string ETag { get; set; }
        public TimeSpan? Age { get; set; }
        public TimeSpan? MaxAge { get; set; }
        public DateTime? Expires { get; set; }
        public DateTime? LastModified { get; set; }
        public CacheControl? CacheControl { get; set; }
    }

    public class CacheEtagServices : Service
    {
        public object Any(SetCache request)
        {
            return new HttpResult(request)
            {
                Age = request.Age,
                ETag = request.ETag,
                MaxAge = request.MaxAge,
                Expires = request.Expires,
                LastModified = request.LastModified,
                CacheControl = request.CacheControl.GetValueOrDefault(CacheControl.None),
            };
        }
    }


    [Route("/gzip/{FileName}")]
    public class DownloadGzipFile : IReturn<byte[]>
    {
        public string FileName { get; set; }
    }

    public class FileServices : Service
    {
        public object Get(DownloadGzipFile request)
        {
            var filePath = HostContext.AppHost.MapProjectPath($"~/img/{request.FileName}");
            if (Request.RequestPreferences.AcceptsGzip)
            {
                var targetPath = string.Concat(filePath, ".gz");
                Compress(filePath, targetPath);

                //var bs = new BufferedStream(File.OpenRead(targetPath), 8192);
                //Response.AddHeader("Content-Type", "application/pdf");
                //Response.AddHeader("Content-Disposition", "attachment; filename=test.pdf");
                //return new GZipStream(bs, CompressionMode.Decompress);

                return new HttpResult(new FileInfo(targetPath))
                {
                    Headers = {
                        { HttpHeaders.ContentDisposition, "attachment; filename=" + request.FileName },
                        { HttpHeaders.ContentEncoding, CompressionTypes.GZip }
                    }
                };
            }

            return new HttpResult(filePath)
            {
                Headers = {
                    { HttpHeaders.ContentDisposition, "attachment; filename=" + request.FileName },
                }
            };
        }

        private void Compress(string readFrom, string writeTo)
        {
            byte[] b;
            using (var f = new FileStream(readFrom, FileMode.Open))
            {
                b = new byte[f.Length];
                f.Read(b, 0, (int)f.Length);
            }

            using (var fs = new FileStream(writeTo, FileMode.OpenOrCreate))
            using (var gz = new GZipStream(fs, CompressionMode.Compress, false))
            {
                gz.Write(b, 0, b.Length);
            }
        }
    }

    [Route("/match/{Language}/{Name*}", Matches = @"PathInfo =~ \/match\/[a-z]{2}\/[A-Za-z]+$")]
    public class MatchName : IReturn<HelloResponse>
    {
        public string Language { get; set; }
        public string Name { get; set; }
    }

    [Route("/match/{Language*}", Matches = @"PathInfo =~ \/match\/[a-z]{2}$")]
    public class MatchLang : IReturn<HelloResponse>
    {
        public string Language { get; set; }
    }

    public class RouteMatchServices : Service
    {
        public HelloResponse Any(MatchName request) => new HelloResponse { Result = request.GetType().Name };
        public HelloResponse Any(MatchLang request) => new HelloResponse { Result = request.GetType().Name };
    }
    
    [Route("/reqlogstest/{Name}")]
    public class RequestLogsTest : IReturn<string>
    {
        public string Name { get; set; }
    }

    public class InProcRequest1 {}
    public class InProcRequest2 {}

    public class RequestLogsServices : Service
    {
        public object Any(RequestLogsTest request)
        {
            Gateway.Publish(new InProcRequest1());
            Gateway.Publish(new InProcRequest2());

            return "hello";
        }

        public object Any(InProcRequest1 request) => "InProcRequest1 response";
        public object Any(InProcRequest2 request) => "InProcRequest2 response";
    }

    [Alias("Rockstar")]
    [NamedConnection("SqlServer")]
    public class NamedRockstar : Rockstar { }

    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            try 
            {
                ServiceStack.Logging.LogManager.LogFactory = new ConsoleLogFactory(debugEnabled: true);
                new AppHost().Init();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw exception;
            }
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

    public static class HtmlHelpers
    {
        public static MvcHtmlString DisplayPrice(this HtmlHelper html, decimal price)
        {
            return MvcHtmlString.Create(price == 0
                ? "<span>FREE!</span>"
                : $"{price:C2}");
        }
    }
}