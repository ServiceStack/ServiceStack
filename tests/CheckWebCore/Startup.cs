using System;
using System.Collections.Generic;
using System.Net;
using Funq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceStack;
using ServiceStack.Api.OpenApi;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.Mvc;
using ServiceStack.NativeTypes.TypeScript;
using ServiceStack.Text;
using ServiceStack.Validation;

namespace CheckWebCore
{
    [Priority(-3)]
    public class TestConfigureServicesSub1 : IConfigureServices
    {
        public void Configure(IServiceCollection services) => "IConfigureServices(-2)".Print(); // #1
    }

    public class TestConfigureServices : IConfigureServices
    {
        public void Configure(IServiceCollection services) => "IConfigureServices(0)".Print();  // #4
    }

    [Priority(-1)]
    public class TestStartup : IStartup
    {
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            "IStartup.ConfigureServices(-1)".Print();                                           // #2
            return null;
        }

        public void Configure(IApplicationBuilder app) => "IStartup.Configure(-1)".Print();     // #7
    }

    public class Startup : ModularStartup
    {
        public Startup(IConfiguration configuration) : base(configuration){}

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public new void ConfigureServices(IServiceCollection services)                              
        {
            "Startup.ConfigureServices(IServiceCollection services)".Print();                   // #3
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)                 
        {
            "Startup.Configure(IApplicationBuilder app, IHostingEnvironment env)".Print();      // #8
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var AppSettings = new NetCoreAppSettings(Configuration);
            AppSettings.GetNullableString("servicestack:license");
            
            app.UseServiceStack(new AppHost
            {
                AppSettings = AppSettings
            });
        }
    }
    
    [Priority(1)]
    public class TestPostConfigureServicesAdd1 : IConfigureServices
    {
        public void Configure(IServiceCollection services) => "IConfigureServices(+1)".Print(); // #5
    }
    [Priority(1)]
    public class TestConfigureAppAdd1 : IConfigureApp
    {
        public void Configure(IApplicationBuilder app)=> "IConfigureApp(+1)".Print();           // #9
    }
    [Priority(-2)]
    public class TestConfigureAppAdd2 : IConfigureApp
    {
        public void Configure(IApplicationBuilder app)=> "IConfigureApp(-2)".Print();           // #6
    }
    

    public class AppHost : AppHostBase
    {
        public AppHost() : base("TestLogin", typeof(MyServices).Assembly) { }

        public override void Configure(IServiceCollection services)
        {
            services.AddSingleton<ICacheClient>(new MemoryCacheClient());
        }

        // http://localhost:5000/auth/credentials?username=testman@test.com&&password=!Abc1234
        // Configure your AppHost with the necessary configuration and dependencies your App needs
        public override void Configure(Container container)
        {
            // enable server-side rendering, see: https://sharpscript.net
            Plugins.Add(new SharpPagesFeature()); 

            if (Config.DebugMode)
            {
                Plugins.Add(new HotReloadFeature());
            }

            Plugins.Add(new RazorFormat()); // enable ServiceStack.Razor
            
            SetConfig(new HostConfig
            {
                AddRedirectParamsToQueryString = true,
                DebugMode = AppSettings.Get(nameof(HostConfig.DebugMode), false),
                UseSameSiteCookies = true, // prevents OAuth providers which use Sessions like Twitter from working
                UseSecureCookies = true,
            });

            var cache = container.Resolve<ICacheClient>();
            
            Plugins.Add(new ValidationFeature());

//            Svg.CssFillColor["svg-auth"] = "#ccc";
            Svg.CssFillColor["svg-icons"] = "#e33";

            this.CustomErrorHttpHandlers[HttpStatusCode.Forbidden] = 
                new SharpPageHandler("/forbidden");

//            TypeScriptGenerator.TypeFilter = (type, args) => {
//                if (type == "ResponseBase`1" && args[0] == "Dictionary<String,List`1>")
//                    return "ResponseBase<Map<string,Array<any>>>";
//                return null;
//            };


            //GetPlugin<SvgFeature>().ValidateFn = req => Config.DebugMode; // only allow in DebugMode
        }
    }
    
    [Exclude(Feature.Metadata)]
    [FallbackRoute("/{PathInfo*}", Matches="AcceptsHtml")]
    public class FallbackForClientRoutes
    {
        public string PathInfo { get; set; }
    }
    
    [Route("/hello")]
    [Route("/hello/{Name}")]
    public class Hello : IReturn<HelloResponse>
    {
        public string Name { get; set; }
    }

    public class HelloResponse
    {
        public string Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/testauth")]
    public class TestAuth : IReturn<TestAuth> {}

    [Route("/session")]
    public class Session : IReturn<AuthUserSession> {}
    
    [Route("/throw")]
    public class Throw {}
    
    [Route("/api/data/import/{Month}", "POST")]
    public class ImportData : IReturn<ImportDataResponse>
    {
        public string Month { get; set; }
    }

    public class ImportDataResponse : IHasResponseStatus
    {
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class ResponseBase<T>
    {
        public T Result { get; set; }
    }
    public class Campaign : IReturn<ResponseBase<Dictionary<string, List<object>>>>
    {
        public int Id { get; set; }
    }
    public class DataEvent
    {
        public int Id { get; set; }
    }

    public class Dummy
    {
        public Campaign Campaign { get; set; }
        public DataEvent DataEvent { get; set; }
    }

        
    //    [Authenticate]
    public class MyServices : Service
    {
        public object Any(Dummy request) => request;
        public object Any(Campaign request) => request;
        
        //Return index.html for unmatched requests so routing is handled on client
        public object Any(FallbackForClientRoutes request) => 
            Request.GetPageResult("/");
//            new PageResult(Request.GetPage("/")) { Args = { [nameof(Request)] = Request } };

        public object Any(Hello request)
        {
            return new HelloResponse { Result = $"Hello, {request.Name}!" };
        }

        public object Any(TestAuth request) => request;

        [Authenticate]
        public object Any(Session request) => SessionAs<AuthUserSession>();

        public object Any(Throw request) => HttpError.Conflict("Conflict message");
//        public object Any(Throw request) => new HttpResult
//            {StatusCode = HttpStatusCode.Conflict, Response = "Error message"};

        [Authenticate]
        public object Post(ImportData request)
        {
            if (Request.Files == null || Request.Files.Length <= 0)
            {
                throw new Exception("No import file was received by the server");
            }

            // This is always coming through as null
            if (request.Month == null)
            {
                throw new Exception("No month was received by the server");
            }

            var file = (HttpFile)Request.Files[0];
            var month = request.Month.Replace('-', '/');

            //ImportData(month, file);

            return new ImportDataResponse();
        }
    }
}