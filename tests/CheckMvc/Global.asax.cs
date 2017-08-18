using System.Configuration;
using System.Data;
using System.Web.Mvc;
using System.Web.Routing;
using Check.ServiceInterface;
using Funq;
using ServiceStack;
using ServiceStack.Api.OpenApi;
using ServiceStack.Configuration;
using ServiceStack.Data;
using ServiceStack.Mvc;
using ServiceStack.OrmLite;
using ServiceStack.Redis;
using ServiceStack.Text;

namespace CheckMvc
{
    public class AppHost : AppHostBase
    {
        public AppHost()
            : base("Check MVC", typeof(ErrorsService).Assembly) {}

        public override void Configure(Container container)
        {
            //Set MVC to use the same Funq IOC as ServiceStack
            ControllerBuilder.Current.SetControllerFactory(new FunqControllerFactory(container));

            container.Register<IRedisClientsManager>(c =>
                new RedisManagerPool());

            container.Register(c => c.Resolve<IRedisClientsManager>().GetCacheClient());

            SetConfig(new HostConfig { DebugMode = true });
            
            Plugins.Add(new TemplatePagesFeature());
            
            Plugins.Add(new OpenApiFeature());
        }
    }

    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        public static IDbConnectionFactory DbFactory;

        protected void Application_Start()
        {
            DbFactory = new OrmLiteConnectionFactory(
                ConfigurationManager.AppSettings["connectionString"],
                SqlServerDialect.Provider);

            JsConfig.DateHandler = DateHandler.ISO8601;
            JsConfig.EmitCamelCaseNames = true;

            AreaRegistration.RegisterAllAreas();

            //WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            new AppHost().Init();
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