using System;
using TestHosts.Common;

namespace location.api.wildcard40
{
	public class Global : System.Web.HttpApplication
	{
		protected void Application_Start(object sender, EventArgs e)
		{
			new TestHostAppHost().Init();
		}
	}
}
