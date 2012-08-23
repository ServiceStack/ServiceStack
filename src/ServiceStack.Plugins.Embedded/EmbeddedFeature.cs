using System;
using ServiceStack.VirtualPath;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.Plugins.Embedded.VirtualPath;

namespace ServiceStack.Plugins.Embedded
{
	public class EmbeddedFeature : IPlugin
	{
		public void Register(IAppHost appHost)
		{
			appHost.VirtualPathProvider = new MultiVirtualPathProvider(appHost, 
				new ResourceVirtualPathProvider(appHost), 
				new FileSystemVirtualPathProvider(appHost));
		}
	}
}

