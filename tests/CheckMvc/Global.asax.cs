using System.Web.Mvc;
using System.Web.Routing;
using Check.ServiceInterface;
using Funq;
using ServiceStack;
using ServiceStack.Mvc;
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
        }
    }

    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            JsConfig.DateHandler = DateHandler.ISO8601;
            JsConfig.EmitCamelCaseNames = true;

            AreaRegistration.RegisterAllAreas();

            //WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            new AppHost().Init();
        }
    }
}