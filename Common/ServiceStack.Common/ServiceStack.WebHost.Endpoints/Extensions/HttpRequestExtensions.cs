using System;
using System.Net;
using System.Web;
using ServiceStack.Logging;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	/**
	 * 
		 Input: http://localhost:96/Cambia3/Temp/Test.aspx/path/info?q=item#fragment

		Some HttpRequest path and URL properties:
		Request.ApplicationPath:	/Cambia3
		Request.CurrentExecutionFilePath:	/Cambia3/Temp/Test.aspx
		Request.FilePath:			/Cambia3/Temp/Test.aspx
		Request.Path:				/Cambia3/Temp/Test.aspx/path/info
		Request.PathInfo:			/path/info
		Request.PhysicalApplicationPath:	D:\Inetpub\wwwroot\CambiaWeb\Cambia3\
		Request.QueryString:		/Cambia3/Temp/Test.aspx/path/info?query=arg
		Request.Url.AbsolutePath:	/Cambia3/Temp/Test.aspx/path/info
		Request.Url.AbsoluteUri:	http://localhost:96/Cambia3/Temp/Test.aspx/path/info?query=arg
		Request.Url.Fragment:	
		Request.Url.Host:			localhost
		Request.Url.LocalPath:		/Cambia3/Temp/Test.aspx/path/info
		Request.Url.PathAndQuery:	/Cambia3/Temp/Test.aspx/path/info?query=arg
		Request.Url.Port:			96
		Request.Url.Query:			?query=arg
		Request.Url.Scheme:			http
		Request.Url.Segments:		/
									Cambia3/
									Temp/
									Test.aspx/
									path/
									info
	 * */
	public static class HttpRequestExtensions
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof (HttpRequestExtensions));

		public static string GetOperationName(this HttpRequest request)
		{
			var pathInfo = request.GetLastPathInfo();
			return GetOperationNameFromLastPathInfo(pathInfo);
		}

		public static string GetOperationNameFromLastPathInfo(string lastPathInfo)
		{
			if (string.IsNullOrEmpty(lastPathInfo)) return null;

			var operationName = lastPathInfo.Substring("/".Length);

			return operationName;
		}

		private static string GetLastPathInfoFromRawUrl(string rawUrl)
		{
			var pathInfo = rawUrl.IndexOf("?") != -1
				? rawUrl.Substring(0, rawUrl.IndexOf("?")) 
				: rawUrl;

			pathInfo = pathInfo.Substring(pathInfo.LastIndexOf("/"));

			return pathInfo;
		}

		public static string GetLastPathInfo(this HttpRequest request)
		{
			var pathInfo = request.PathInfo;
			if (string.IsNullOrEmpty(pathInfo))
			{
				pathInfo = GetLastPathInfoFromRawUrl(request.RawUrl);
			}
			
			//Log.DebugFormat("Request.PathInfo: {0}, Request.RawUrl: {1}, pathInfo:{2}",
			//    request.PathInfo, request.RawUrl, pathInfo);

			return pathInfo;
		}

		public static string GetUrlHostName(this HttpRequest request)
		{
			//TODO: Fix bug in mono fastcgi, when trying to get 'Request.Url.Host'
			try
			{
				return request.Url.Host;
			}
			catch (Exception ex)
			{
				Log.ErrorFormat("Error trying to get 'Request.Url.Host'", ex);
	
				return request.UserHostName;
			}
		}

		// http://localhost/ServiceStack.Examples.Host.Web/Public/Public/Soap12/Wsdl => 
		// http://localhost/ServiceStack.Examples.Host.Web/Public/Soap12/
		public static string GetParentBaseUrl(this HttpRequest request)
		{
			var rawUrl = request.RawUrl; // /Cambia3/Temp/Test.aspx/path/info
			var endpointsPath = rawUrl.Substring(0, rawUrl.LastIndexOf('/') + 1);  // /Cambia3/Temp/Test.aspx/path
			return GetAuthority(request) + endpointsPath;
		}

		//=> http://localhost:96 ?? ex=> http://localhost
		private static string GetAuthority(HttpRequest request)
		{
			try
			{
				return request.Url.GetLeftPart(UriPartial.Authority);
			}
			catch (Exception ex)
			{
				Log.Error("Error trying to get: request.Url.GetLeftPart(UriPartial.Authority): " + ex.Message, ex);
				return "http://" + request.UserHostName;
			}
		}

		public static string GetOperationName(this HttpListenerRequest request)
		{
			return request.Url.Segments[request.Url.Segments.Length - 1];
		}

		public static string GetLastPathInfo(this HttpListenerRequest request)
		{
			return GetLastPathInfoFromRawUrl(request.RawUrl);
		}
	}
}