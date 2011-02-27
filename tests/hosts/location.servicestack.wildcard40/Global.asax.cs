using System;
using TestHosts.Common;

namespace location.servicestack.wildcard40
{
	public class Global : System.Web.HttpApplication
	{
		protected void Application_Start(object sender, EventArgs e)
		{
			new TestHostAppHost().Init();
		}
	}

}
