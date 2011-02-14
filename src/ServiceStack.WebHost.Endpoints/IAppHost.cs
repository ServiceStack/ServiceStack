using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints
{
	public interface IAppHost
	{
		T TryResolve<T>();

		IContentTypeFilter ContentTypeFilters { get; }
		
		List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters { get; }
		
		List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters { get; }

		EndpointHostConfig Config { get; }
	}
}