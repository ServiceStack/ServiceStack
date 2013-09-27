using System;
using System.Net;
using System.Reflection;
using ServiceStack.Support.WebHost;
using ServiceStack.Utils;
using ServiceStack.WebHost;
using ServiceStack.WebHost.Handlers;
using ServiceStack.WebHost.HttpListener;

namespace ServiceStack
{
	/// <summary>
	/// Inherit from this class if you want to host your web services inside a 
	/// Console Application, Windows Service, etc.
	/// 
	/// Usage of HttpListener allows you to host webservices on the same port (:80) as IIS 
	/// however it requires admin user privillages.
	/// </summary>
	public abstract class AppHostHttpListenerBase 
		: HttpListenerBase
	{
		protected AppHostHttpListenerBase() {}

		protected AppHostHttpListenerBase(string serviceName, params Assembly[] assembliesWithServices)
			: base(serviceName, assembliesWithServices)
		{
			AppHostConfig.Instance.ServiceStackHandlerFactoryPath = null;
			AppHostConfig.Instance.MetadataRedirectPath = "metadata";
		}

		protected AppHostHttpListenerBase(string serviceName, string handlerPath, params Assembly[] assembliesWithServices)
			: base(serviceName, assembliesWithServices)
		{
			AppHostConfig.Instance.ServiceStackHandlerFactoryPath = string.IsNullOrEmpty(handlerPath)
				? null : handlerPath;			
			AppHostConfig.Instance.MetadataRedirectPath = handlerPath == null 
				? "metadata"
				: PathUtils.CombinePaths(handlerPath, "metadata");
		}

		protected override void ProcessRequest(HttpListenerContext context)
		{
			if (string.IsNullOrEmpty(context.Request.RawUrl)) return;

			var operationName = context.Request.GetOperationName();

			var httpReq = new ListenerRequest(operationName, context.Request);
			var httpRes = new ListenerResponse(context.Response);
			var handler = HttpHandlerFactory.GetHandler(httpReq);

			var serviceStackHandler = handler as IServiceStackHttpHandler;
			if (serviceStackHandler != null)
			{
				var restHandler = serviceStackHandler as RestHandler;
				if (restHandler != null)
				{
					httpReq.OperationName = operationName = restHandler.RestPath.RequestType.Name;
				}
				serviceStackHandler.ProcessRequest(httpReq, httpRes, operationName);
				httpRes.Close();
				return;
			}

			throw new NotImplementedException("Cannot execute handler: " + handler + " at PathInfo: " + httpReq.PathInfo);
		}

	}
}