using System;
using System.Collections.Generic;
using Funq;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.ServiceInterface.Testing
{
	public class BasicAppHost : IAppHost
	{
		public BasicAppHost()
		{
			this.Container = new Container();
			this.RequestFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
			this.ResponseFilters = new List<Action<IHttpRequest, IHttpResponse, object>>();
			this.HtmlProviders = new List<StreamSerializerResolverDelegate>();
			this.CatchAllHandlers = new List<HttpHandlerResolverDelegate>();
		}

		public T TryResolve<T>()
		{
			return this.Container.TryResolve<T>();
		}

		public Container Container { get; set; }

		public IContentTypeFilter ContentTypeFilters { get; set; }

		public List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters { get; set; }

		public List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters { get; set; }

		public List<StreamSerializerResolverDelegate> HtmlProviders { get; set; }

		public List<HttpHandlerResolverDelegate> CatchAllHandlers { get; set; }

		public EndpointHostConfig Config { get; set; }
	}
}