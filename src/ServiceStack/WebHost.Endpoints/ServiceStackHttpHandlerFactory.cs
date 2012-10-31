using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Web;
using ServiceStack.Common;
using ServiceStack.MiniProfiler.UI;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Metadata;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
{
	public class ServiceStackHttpHandlerFactory
		: IHttpHandlerFactory
	{

		[ThreadStatic]
		public static string DebugLastHandlerArgs;

		private static ServiceStackHttpHandlerFactoryInstance _instance = new ServiceStackHttpHandlerFactoryInstance(EndpointHost.SingletonInstance);
		
		[ThreadStatic]
		private static ServiceStackHttpHandlerFactoryInstance _threadSpecificInstance;
		private static ServiceStackHttpHandlerFactoryInstance Instance
		{
			get
			{
				var ts = _threadSpecificInstance;
				if (ts != null) return ts;
				else return _instance;
			}
		}

		internal static IDisposable SetThreadSpecificHost(EndpointHostInstance useInstance)
		{
			_threadSpecificInstance = new ServiceStackHttpHandlerFactoryInstance(useInstance);
			return new ThreadCleanup();
		}

		private class ThreadCleanup : IDisposable
		{
			public void Dispose()
			{
				ServiceStackHttpHandlerFactory._threadSpecificInstance = null;
			}
		}

		// Entry point for ASP.NET
		public IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
		{
			return Instance.GetHandler(context, requestType, url, pathTranslated);
		}

		public static string GetBaseUrl()
		{
			return Instance.GetBaseUrl();
		}

		// Entry point for HttpListener
		public static IHttpHandler GetHandler(IHttpRequest httpReq)
		{
			return Instance.GetHandler(httpReq);
		}

		public static IHttpHandler GetHandlerForPathInfo(string httpMethod, string pathInfo, string requestPath, string filePath)
		{
			return Instance.GetHandlerForPathInfo(httpMethod, pathInfo, requestPath, filePath);
		}

		public void ReleaseHandler(IHttpHandler handler)
		{
			Instance.ReleaseHandler(handler);
		}
	}
}