using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Reflection;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Support;

namespace ServiceStack.WebHost.Endpoints
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
		private readonly ILog log = LogManager.GetLogger(typeof(AppHostHttpListenerBase));

		protected AppHostHttpListenerBase()
		{
		}

		protected AppHostHttpListenerBase(string serviceName, params Assembly[] assembliesWithServices) 
			: base(serviceName, assembliesWithServices)
		{
		}

		protected override void ProcessRequest(HttpListenerContext context)
		{
			if (string.IsNullOrEmpty(context.Request.RawUrl)) return;

			var operationName = context.Request.GetOperationName();
			var httpMethod = context.Request.HttpMethod;
			var queryString = context.Request.QueryString;
			var inputStream = context.Request.InputStream;
			var response = new HttpListenerResponseWrapper(context.Response);

			var url = context.Request.Url.PathAndQuery.ToLower();
			if (url.Contains("/xml/"))
			{
				ProcessXmlRequest(url, operationName, httpMethod, queryString, inputStream, response);
			}
			else if (url.Contains("/json/"))
			{
				ProcessJsonRequest(url, operationName, httpMethod, queryString, inputStream, response);
			}
			else if (url.Contains("/jsv/"))
			{
				ProcessJsvRequest(url, operationName, httpMethod, queryString, inputStream, response);
			}
		}

		private void ProcessJsvRequest(string url, string operationName, string httpMethod, NameValueCollection queryString, Stream inputStream, HttpListenerResponseWrapper response)
		{
			try
			{
				var request = JsvHandlerBase.CreateRequest(operationName, httpMethod, queryString, null, inputStream);

				var isDebugRequest = queryString["debug"] != null;
				
				var writeFn = isDebugRequest
					? (Func<object, string>)JsvFormatter.SerializeAndFormat
					: TypeSerializer.SerializeToString;
				
				var contentType = isDebugRequest ? ContentType.PlainText : ContentType.JsvText;

				if (url.Contains("/jsv/syncreply/"))
				{
					var result = ExecuteService(request, EndpointAttributes.SyncReply | EndpointAttributes.Jsv | HttpMethods.GetEndpointAttribute(httpMethod));
					response.WriteToResponse(result, writeFn, contentType);
				}
				else if (url.Contains("/jsv/asynconeway/"))
				{
					ExecuteService(request, EndpointAttributes.AsyncOneWay | EndpointAttributes.Jsv | HttpMethods.GetEndpointAttribute(httpMethod));
				}
			}
			catch (Exception ex)
			{
				log.Error(ex);
				response.WriteJsvErrorToResponse(operationName, ex.Message, ex);
			}
		}

		private void ProcessJsonRequest(string url, string operationName, string httpMethod, NameValueCollection queryString, Stream inputStream, HttpListenerResponseWrapper response)
		{
			try
			{
				var request = JsonHandlerBase.CreateRequest(operationName,
				                                            httpMethod, queryString, null, inputStream);

				if (url.Contains("/json/syncreply/"))
				{
					var result = ExecuteService(request, EndpointAttributes.SyncReply | EndpointAttributes.Json | HttpMethods.GetEndpointAttribute(httpMethod));
					response.WriteToResponse(result, x => JsonDataContractSerializer.Instance.Parse(result), ContentType.Json);
				}
				else if (url.Contains("/json/asynconeway/"))
				{
					var result = ExecuteService(request, EndpointAttributes.AsyncOneWay | EndpointAttributes.Json | HttpMethods.GetEndpointAttribute(httpMethod));
				}
			}
			catch (Exception ex)
			{
				log.Error(ex);
				response.WriteJsonErrorToResponse(operationName, ex.Message, ex);
			}
		}

		private void ProcessXmlRequest(string url, string operationName, string httpMethod, NameValueCollection queryString, Stream inputStream, HttpListenerResponseWrapper response)
		{
			try
			{
				var request = XmlHandlerBase.CreateRequest(operationName, 
				                                           httpMethod, queryString, inputStream);

				if (url.Contains("/xml/syncreply/"))
				{
					var result = ExecuteService(request, EndpointAttributes.SyncReply | EndpointAttributes.Xml | HttpMethods.GetEndpointAttribute(httpMethod));
					response.WriteToResponse(result, x => DataContractSerializer.Instance.Parse(result), ContentType.Xml);
				}
				else if (url.Contains("/xml/asynconeway/"))
				{
					var result = ExecuteService(request, EndpointAttributes.AsyncOneWay | EndpointAttributes.Xml | HttpMethods.GetEndpointAttribute(httpMethod));
				}
			}
			catch (Exception ex)
			{
				log.Error(ex);
				response.WriteXmlErrorToResponse(operationName, ex.Message, ex);
			}
		}

	}
}