using System;

namespace ServiceStack.AuthWeb.Tests
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            log4net.Config.XmlConfigurator.Configure();
            new AppHost().Init();
        }
    }
}