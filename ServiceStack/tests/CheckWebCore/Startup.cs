using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceStack;
using ServiceStack.Api.OpenApi;
using ServiceStack.Auth;
using ServiceStack.Caching;
using ServiceStack.Configuration;
using ServiceStack.DataAnnotations;
using ServiceStack.FluentValidation.Validators;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.Mvc;
using ServiceStack.NativeTypes.CSharp;
using ServiceStack.NativeTypes.TypeScript;
using ServiceStack.Script;
using ServiceStack.Text;
using ServiceStack.Validation;
using ServiceStack.Web;
using Container = Funq.Container;

namespace CheckWebCore
{
    [Priority(-1)]
    public class MyPreConfigureServices : IConfigureServices
    {
        public void Configure(IServiceCollection services) => "#1".Print(); // #1
    }

    public class MyConfigureServices : IConfigureServices
    {
        public void Configure(IServiceCollection services) => "#4".Print();  // #4
    }

    [Priority(-1)]
    public class MyStartup : IStartup
    {
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            "#2".Print();                                           // #2
            return null;
        }

        public void Configure(IApplicationBuilder app) => "#6".Print();     // #6
    }

    public class Startup : ModularStartup
    {
        public Startup(IConfiguration configuration) : base(configuration)
        {
//            IgnoreTypes.Add(typeof(MyStartup));
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public new void ConfigureServices(IServiceCollection services)                              
        {
            "#3".Print();                   // #3
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)                 
        {
            "#8".Print();      // #8
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
        }
    }
    
    [Priority(1)]
    public class MyPostConfigureServices : IConfigureServices
    {
        public void Configure(IServiceCollection services) => "#5".Print(); // #5
    }

    [Priority(-1)]
    public class MyPreConfigureApp : IConfigureApp
    {
        public void Configure(IApplicationBuilder app)=> "#7".Print();           // #7
    }
    public class MyConfigureApp : IConfigureApp
    {
        public void Configure(IApplicationBuilder app)=> "#9".Print();           // #9
    }


    public interface IAppHostConstraint{}
    public class AppHostConstraint : IAppHostConstraint{}
    public abstract class AppHostConstraintsBase<TServiceInterfaceAssembly> : AppHostBase
        where TServiceInterfaceAssembly : IAppHostConstraint
    {
        protected AppHostConstraintsBase(string serviceName, params Assembly[] assembliesWithServices) 
        : base(serviceName, assembliesWithServices)
        {
            ConsoleLogFactory.Configure();
        }
    }

    public class AppHost 
        //: AppHostConstraintsBase<AppHostConstraint>, IConfigureApp
        : AppHostBase, IConfigureApp
    {
        public AppHost() : base("TestLogin", typeof(MyServices).Assembly) { }

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseServiceStack(new AppHost
            {
                //PathBase = "/test",
            });
        }

        public override void Configure(IServiceCollection services)
        {
            services.AddServiceStackGrpc();
            services.AddSingleton<ICacheClient>(new MemoryCacheClient());
        }
        
        // http://localhost:5000/auth/credentials?username=testman@test.com&&password=!Abc1234
        // Configure your AppHost with the necessary configuration and dependencies your App needs
        public override void Configure(Container container)
        {
            SetConfig(new HostConfig
            {
                AddRedirectParamsToQueryString = true,
                //DebugMode = AppSettings.Get(nameof(HostConfig.DebugMode), false),
                DebugMode = true,
                // DebugMode = false,
//                UseSameSiteCookies = true, // prevents OAuth providers which use Sessions like Twitter from working
                UseSecureCookies = true,
                AdminAuthSecret = "secretz",
                CompressFilesWithExtensions = { "js", "css" },
                UseCamelCase = false,
            });
            
            RegisterService<GetFileService>();

            Plugins.Add(new GrpcFeature(App));
            
            // enable server-side rendering, see: https://sharpscript.net
            Plugins.Add(new SharpPagesFeature {
                ScriptMethods = { new CustomScriptMethods() },
                EnableHotReload = false,
            }); 
            
            Plugins.Add(new ServerEventsFeature());
            Plugins.Add(new LispReplTcpServer {
//                RequireAuthSecret = true,
                AllowScriptingOfAllTypes = true,
            });
            
            ConfigurePlugin<UiFeature>(feature => {
                feature.Info.BrandIcon.Uri = "https://vue-ssg.jamstacks.net/assets/img/logo.svg";
            });

            //not needed for /wwwroot/ui 
            //Plugins.AddIfDebug(new HotReloadFeature());

            Plugins.Add(new RazorFormat()); // enable ServiceStack.Razor

            var cache = container.Resolve<ICacheClient>();
            
//            Svg.CssFillColor["svg-auth"] = "#ccc";
            Svg.CssFillColor["svg-icons"] = "#e33";

            this.CustomErrorHttpHandlers[HttpStatusCode.NotFound] = new RazorHandler("/notfound");
            this.CustomErrorHttpHandlers[HttpStatusCode.Forbidden] = new SharpPageHandler("/forbidden");

            Svg.Load(RootDirectory.GetDirectory("/assets/svg"));
            
            Plugins.Add(new PostmanFeature());

            var nativeTypesFeature = GetPlugin<NativeTypesFeature>();
            nativeTypesFeature
                .ExportAttribute<BindableAttribute>(attr => {
                    var metaAttr = nativeTypesFeature.GetGenerator().ToMetadataAttribute(attr);
                    return metaAttr;
                });
            

            CSharpGenerator.TypeFilter = (type, args) => {
                if (type == "ResponseBase`1" && args[0] == "Dictionary<String,List`1>")
                    return "ResponseBase<Dictionary<string,List<object>>>";
                return null;
            };

            TypeScriptGenerator.TypeFilter = (type, args) => {
                if (type == "ResponseBase`1" && args[0] == "Dictionary<String,List`1>")
                    return "ResponseBase<Map<string,Array<any>>>";
                return null;
            };

            TypeScriptGenerator.DeclarationTypeFilter = (type, args) => {
                return null;
            };


            //GetPlugin<SvgFeature>().ValidateFn = req => Config.DebugMode; // only allow in DebugMode
        }
    }

    public class CustomScriptMethods : ScriptMethods
    {
        public ITypeValidator CustomTypeValidator(string arg) => null;
        public IPropertyValidator CustomPropertyValidator(string arg) => null;
    }
    
    [Exclude(Feature.Metadata)]
    [FallbackRoute("/{PathInfo*}", Matches="AcceptsHtml")]
    public class FallbackForClientRoutes
    {
        public string PathInfo { get; set; }
    }
    
    [Route("/hello")]
    [Route("/hello/{Name}")]
    public class Hello : IReturn<HelloResponse>, IGet
    {
        public string Name { get; set; }
    }

    public class HelloResponse
    {
        public string Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Restrict(VisibleLocalhostOnly = true)]
    [Route("/testauth")]
    [Tag("mobile")]
    public class TestAuth : IReturn<TestAuth> {}

    [Route("/session")]
    public class Session : IReturn<AuthUserSession> {}
    
    [Restrict(VisibilityTo = RequestAttributes.Localhost)]
    [Route("/throw")]
    [Tag("desktop")]
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
    [Tag("web")]
    public class Campaign : IReturn<ResponseBase<Dictionary<string, List<object>>>>
    {
        public int Id { get; set; }
    }
    public class DataEvent
    {
        public int Id { get; set; }
    }
    
    public enum EnumMemberTest
    {
        [EnumMember(Value = "No ne")] None = 0,
        [EnumMember(Value = "Template")] Template = 1,
        [EnumMember(Value = "Rule")] Rule = 3,
    }

    public class Dummy
    {
        public Campaign Campaign { get; set; }
        public DataEvent DataEvent { get; set; }
        public ExtendsDictionary ExtendsDictionary { get; set; }
        public EnumMemberTest EnumMemberTest { get; set; }
    }
   
    public class ExtendsDictionary : Dictionary<Guid, string> {
    }
    
    [Route("/hello/body")]
    public class HelloBody : IReturn<HelloBodyResponse>
    {
        public string Name { get; set; }
    }

    public class HelloBodyResponse
    {
        public string Message { get; set; }
        public string Body { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/bookings/repeat",
        Summary = "Create new bookings",
        Notes = "Create new bookings if you are authorized to do so.",
        Verbs = "POST")]
    [ApiResponse(HttpStatusCode.Unauthorized, "You were unauthorized to call this service")]
    //[Restrict(VisibleLocalhostOnly = true)]
    [Tag("web"),Tag("mobile"),Tag("desktop")]
    public class CreateBookings : CreateBookingBase ,IReturn<CreateBookingsResponse>
    {

        [ApiMember(
        Description =
        "Set the dates you want to book and it's quantities. It's an array of dates and quantities.",
        IsRequired = true)]
        public List<DatesToRepeat> DatesToRepeat { get; set; }

        [ApiMember]
        public IEnumerable<DatesToRepeat> DatesToRepeatIEnumerable { get; set; }
        [ApiMember]
        public DatesToRepeat[] DatesToRepeatArray { get; set; }
    }

    public class CreateBookingBase
    {
        public int Id { get; set; }
    }

    public class CreateBookingsResponse
    {
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class DatesToRepeat
    {
        public int Ticks { get; set; }
    }
    
    
    [Route("/swagger/search", "POST")]
    public class SwaggerSearch : IReturn<EmptyResponse>, IPost
    {
        public List<SearchFilter> Filters { get; set; }
    }

    public class SearchFilter
    {
        [ApiMember(Name = "Field")]
        public string Field { get; set; }

        [ApiMember(Name = "Values")]
        public List<string> Values { get; set; }

        [ApiMember(Name = "Type")]
        public string Type { get; set; }
    }
    
    [ValidateIsAuthenticated]
    [ValidateIsAdmin]
    [ValidateHasRole("TheRole")]
    [ValidateHasPermission("ThePerm")]
    public class TriggerAllValidators 
        : IReturn<IdResponse>
    {
        [ValidateCreditCard]
        public string CreditCard { get; set; }
        [ValidateEmail]
        public string Email { get; set; }
        [ValidateEmpty]
        public string Empty { get; set; }
        [ValidateEqual("Equal")]
        public string Equal { get; set; }
        [ValidateExclusiveBetween(10, 20)]
        public int ExclusiveBetween { get; set; }
        [ValidateGreaterThanOrEqual(10)]
        public int GreaterThanOrEqual { get; set; }
        [ValidateGreaterThan(10)]
        public int GreaterThan { get; set; }
        [ValidateInclusiveBetween(10, 20)]
        public int InclusiveBetween { get; set; }
        [ValidateExactLength(10)]
        public string Length { get; set; }
        [ValidateLessThanOrEqual(10)]
        public int LessThanOrEqual { get; set; }
        [ValidateLessThan(10)]
        public int LessThan { get; set; }
        [ValidateNotEmpty]
        public string NotEmpty { get; set; }
        [ValidateNotEqual("NotEqual")]
        public string NotEqual { get; set; }
        [ValidateNull]
        public string Null { get; set; }
        [ValidateRegularExpression("^[a-z]*$")]
        public string RegularExpression { get; set; }
        [ValidateScalePrecision(1,1)]
        public decimal ScalePrecision { get; set; }
    }

    [EmitCSharp("[Validate]")]
    [EmitTypeScript("@Validate()")]
    [EmitCode(Lang.Swift | Lang.Dart, "@validate()")]
    public class User : IReturn<User>
    {
        [EmitCSharp("[IsNotEmpty]","[IsEmail]")]
        [EmitTypeScript("@IsNotEmpty()", "@IsEmail()")]
        [EmitCode(Lang.Swift | Lang.Dart, new[]{ "@isNotEmpty()", "@isEmail()" })]
        public string Email { get; set; }
    }


    [ValidateIsAuthenticated]
    [Route("/helloauth/{Name}")]
    public class HelloAuth : IReturn<HelloResponse>
    {
        public string Name { get; set; }
    }

    [ValidateHasRole("TheRole")]
    [Route("/hellorole/{Name}")]
    public class HelloRole : IReturn<HelloResponse>
    {
        public string Name { get; set; }
    }

    //    [Authenticate]
    public class MyServices : Service
    {
        public object Any(User request) => request;
        public object Any(CreateBookings request) => new CreateBookingsResponse();
        
        public object Any(Dummy request) => request;
        public object Any(Campaign request) => request;
        
        //Return index.html for unmatched requests so routing is handled on client
        public object Any(FallbackForClientRoutes request) => 
            Request.GetPageResult("/");
//            new PageResult(Request.GetPage("/")) { Args = { [nameof(Request)] = Request } };

        HelloResponse CreateResponse(object request, string name) =>
            new() { Result = $"{request.GetType().Name}, {name}!" };
        
        public object Any(Hello request) => CreateResponse(request, request.Name);
        public object Any(HelloAuth request) => CreateResponse(request, request.Name);
        public object Any(HelloRole request) => CreateResponse(request, request.Name);

        public object Any(TestAuth request) => request;

        // [Authenticate]
        // public object Any(Session request) => SessionAs<AuthUserSession>();

        public object Any(Throw request) => HttpError.Conflict("Conflict message");
//        public object Any(Throw request) => new HttpResult
//            {StatusCode = HttpStatusCode.Conflict, Response = "Error message"};

        public object Any(TriggerAllValidators request) => new IdResponse();

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

        public object Any(HelloBody request)
        {
            var body = Request.GetRawBody();
            var to = new HelloBodyResponse {
                Message = $"Hello, {request.Name}",
                Body = body,
            };
            return to;
        }

        public object Any(ImpersonateUser request)
        {
            using var service = base.ResolveService<AuthenticateService>();
#pragma warning disable CS0618
            service.Post(new Authenticate { provider = "logout" });
                
            return service.Post(new Authenticate {
                provider = AuthenticateService.CredentialsProvider,
                UserName = request.UserName,
            });
#pragma warning restore CS0618
        }
        
        public object Any(SwaggerSearch request) => new EmptyResponse();
    }
    
    // [RequiredRole("Admin")]
    [Restrict(InternalOnly=true)]
    [Route("/impersonate/{UserName}")]
    public class ImpersonateUser
    {
        public string UserName { get; set; }
    }

        
    [Route("/sse-stats")]
    public class GetSseStats {}

    public class ServerEventsStats : Service
    {
        public IServerEvents ServerEvents { get; set; }

        public object Any(GetSseStats request)
        {
            return ServerEvents.GetStats();
        }
    }

}

