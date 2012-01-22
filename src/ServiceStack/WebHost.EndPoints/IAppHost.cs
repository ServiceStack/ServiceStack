using System;
using System.Collections.Generic;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints
{
	/// <summary>
	/// ASP.NET or HttpListener ServiceStack host
	/// </summary>
	public interface IAppHost : IResolver
	{
		/// <summary>
		/// Register dependency in AppHost IOC on Startup
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="instance"></param>
		void Register<T>(T instance);
		
		/// <summary>
		/// AutoWired Registration of an interface with a concrete type in AppHost IOC on Startup.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="TAs"></typeparam>
		void RegisterAs<T, TAs>() where T : TAs;

		/// <summary>
		/// Register custom ContentType serializers
		/// </summary>
		IContentTypeFilter ContentTypeFilters { get; }
		
		/// <summary>
		/// Add Request Filters
		/// </summary>
		List<Action<IHttpRequest, IHttpResponse, object>> RequestFilters { get; }
		
		/// <summary>
		/// Add Response Filters
		/// </summary>
		List<Action<IHttpRequest, IHttpResponse, object>> ResponseFilters { get; }

		/// <summary>
		/// Add alternative HTML View Engines
		/// </summary>
		List<StreamSerializerResolverDelegate> HtmlProviders { get; }

		/// <summary>
		/// Provide a catch-all handler that doesn't match any routes
		/// </summary>
		List<HttpHandlerResolverDelegate> CatchAllHandlers { get; }

		/// <summary>
		/// The AppHost config
		/// </summary>
		EndpointHostConfig Config { get; }

		/// <summary>
		/// Register an Adhoc web service on Startup
		/// </summary>
		/// <param name="serviceType"></param>
		/// <param name="atRestPaths"></param>
		void RegisterService(Type serviceType, params string[] atRestPaths);
	}
}