using System.Web.Mvc;
using System.Web.Routing;
using Check.ServiceInterface;
using Funq;
using ServiceStack;

namespace CheckMvc
{
    public class AppHost : AppHostBase
    {
        public AppHost()
            : base("Check MVC", typeof(ErrorsService).Assembly) {}

        public override void Configure(Container container)
        {
        }
    }

    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            //WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            new AppHost().Init();
        }
    }
}