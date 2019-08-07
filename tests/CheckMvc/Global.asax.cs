using System;
using System.Configuration;
using System.Data;
using System.IO;
using System.Runtime.Serialization;
using System.Web.Mvc;
using System.Web.Routing;
using Check.ServiceInterface;
using Funq;
using ServiceStack;
using ServiceStack.Api.OpenApi;
using ServiceStack.Data;
using ServiceStack.MiniProfiler;
using ServiceStack.Mvc;
//using ServiceStack.OrmLite; // ref source packages 
using ServiceStack.Redis;
using ServiceStack.Text;
using ServiceStack.Web;

namespace CheckMvc
{
    public class AppHost : AppHostBase
    {
        public AppHost()
            : base("Check MVC", typeof(ErrorsService).Assembly, typeof(SmrImportServices).Assembly) {}

        public override void Configure(Container container)
        {
            //Set MVC to use the same Funq IOC as ServiceStack
            ControllerBuilder.Current.SetControllerFactory(new FunqControllerFactory(container));
            
            Plugins.Add(new MiniProfilerFeature());

//            container.Register<IRedisClientsManager>(c =>
//                new RedisManagerPool());

//            container.Register(c => c.Resolve<IRedisClientsManager>().GetCacheClient());

            SetConfig(new HostConfig { DebugMode = true });
            
            Plugins.Add(new SharpPagesFeature());
            
            Plugins.Add(new OpenApiFeature());
        }
    }

    public class TestGateway : IReturn<TestGatewayResponse>
    {
        public string Name { get; set; }
    }

    public class TestGatewayResponse
    {
        public string Result { get; set; }
        
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class TestGatewayService : Service
    {
        public object Any(TestGateway request) =>
            throw HttpError.NotFound("NotFound");
    }

    public class SmrImportServices : Service
    {
        public object Post(AddSmrImportRequest request)
        {
            return request;
        }
    }
    
    [ServiceStack.Route("/addsmrimportrequest/{Year}/{Month}/{ScheduledDay}/{ScheduledMonth}/{ScheduledYear}/{ScheduledHour}/{ScheduledMinutes}/{AuditUserName}/{AuditIpAddress}", 
        Verbs = "OPTIONS POST")]
    [DataContract]
    public class AddSmrImportRequest : QueryBase, IRequiresRequestStream, IReturn<AddSmrImportRequest>
    {
        [DataMember(IsRequired = true, Order = 1)]
        public int Year { get; set; }

        [DataMember(IsRequired = true, Order = 2)]
        public short Month { get; set; }

        [DataMember(IsRequired = true, Order = 3)]
        public short ScheduledDay { get; set; }

        [DataMember(IsRequired = true, Order = 4)]
        public short ScheduledMonth { get; set; }

        [DataMember(IsRequired = true, Order = 5)]
        public int ScheduledYear { get; set; }

        [DataMember(IsRequired = true, Order = 6)]
        public short ScheduledHour { get; set; }

        [DataMember(IsRequired = true, Order = 7)]
        public short ScheduledMinutes { get; set; }

        [DataMember(IsRequired = true, Order = 8)]
        public string AuditUserName { get; set; }

        [DataMember(IsRequired = true, Order = 9)]
        public string AuditIpAddress { get; set; }

        public Stream RequestStream { get; set; }
    }

    [ServiceStack.Route("/urlcheck")]
    [ServiceStack.Route("/urlcheck/{Name}")]
    public class UrlCheck : IReturn<UrlCheckResponse>
    {
        public string Name { get; set; }
    }

    public class UrlCheckResponse
    {
        public string Result { get; set; }
    }

    public class MyServices : Service
    {
        public object Any(UrlCheck request)
        {
            return new UrlCheckResponse {
                Result = request.ToAbsoluteUri()
            };
        }
    }
    
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        public static IDbConnectionFactory DbFactory;

        protected void Application_Start()
        {
//            DbFactory = new OrmLiteConnectionFactory(
//                ConfigurationManager.AppSettings["connectionString"],
//                SqlServerDialect.Provider);

            JsConfig.Init(new ServiceStack.Text.Config {
                TextCase = TextCase.CamelCase,
                DateHandler = DateHandler.ISO8601,
            });

            AreaRegistration.RegisterAllAreas();

            //WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

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

    public abstract class ControllerBase : Controller
    {
        private IDbConnection db;
        public IDbConnection Db => db ?? (db = MvcApplication.DbFactory.OpenDbConnection());

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            db?.Close();
        }
    }
}