using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using ServiceStack.Configuration;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.WebHost.Endpoints;
using TestHosts.Common;

namespace handler.all40
{
	public class Global : System.Web.HttpApplication
	{
		protected void Application_Start(object sender, EventArgs e)
		{
			new TestHostAppHost().Init();
		}
	}

}
