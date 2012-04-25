using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Funq;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.RazorHostTests
{
	public class AppHost : AppHostBase
	{
		public AppHost() 
			: base("Razor Test", typeof(AppHost).Assembly) {}

		public override void Configure(Container container)
		{			
		}
	}

	public class Global : System.Web.HttpApplication
	{

		void Application_Start(object sender, EventArgs e)
		{
			// Code that runs on application startup

		}

		void Application_End(object sender, EventArgs e)
		{
			//  Code that runs on application shutdown

		}

		void Application_Error(object sender, EventArgs e)
		{
			// Code that runs when an unhandled error occurs

		}

	}
}
