using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using ServiceStack.Logging.NLogger;

namespace ServiceStack.Razor2.Tests
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start( object sender, EventArgs e )
        {
            ServiceStack.Logging.LogManager.LogFactory = new NLogFactory();
            new HelloAppHost().Init();
        }

        protected void Session_Start( object sender, EventArgs e )
        {

        }

        protected void Application_BeginRequest( object sender, EventArgs e )
        {

        }

        protected void Application_AuthenticateRequest( object sender, EventArgs e )
        {

        }

        protected void Application_Error( object sender, EventArgs e )
        {

        }

        protected void Session_End( object sender, EventArgs e )
        {

        }

        protected void Application_End( object sender, EventArgs e )
        {

        }
    }
}
