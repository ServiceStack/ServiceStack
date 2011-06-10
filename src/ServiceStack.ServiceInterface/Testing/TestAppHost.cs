using System;
using System.Collections.Generic;
using System.Reflection;
using Funq;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface.Testing
{
	public class TestAppHost : IAppHost
	{
		private readonly Funq.Container container;

		public TestAppHost()
			: this(new Container(), Assembly.GetExecutingAssembly()) {}

		public TestAppHost(Funq.Container container, params Assembly[] serviceAssemblies)
		{
			this.container = container ?? new Container();
			if (serviceAssemblies.Length == 0)
				serviceAssemblies = new[] { Assembly.GetExecutingAssembly() };
			
			var createInstance = EndpointHostConfig.Instance;

			this.Config = EndpointHost.Config = new EndpointHostConfig {
				ServiceName = GetType().Name,
				ServiceManager = new ServiceManager(true, serviceAssemblies),
			};
			this.ContentTypeFilters = new HttpResponseFilter();
			this.RequestFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
			this.ResponseFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
			this.HtmlProviders = new List<StreamSerializerResolverDelegate>();
			this.CatchAllHandlers = new List<HttpHandlerResolverDelegate>();
		}

		public T TryResolve<T>()
		{
			return container.TryResolve<T>();
		}

		public IContentTypeFilter ContentTypeFilters { get; set; }

		public List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters { get; set; }

		public List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters { get; set; }

		public List<StreamSerializerResolverDelegate> HtmlProviders { get; private set; }

		public List<HttpHandlerResolverDelegate> CatchAllHandlers { get; private set; }

		public EndpointHostConfig Config { get; set; }
	}
}