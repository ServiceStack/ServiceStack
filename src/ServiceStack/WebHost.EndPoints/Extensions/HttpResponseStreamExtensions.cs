using System;
using System.Net;
using System.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceHost;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	public static class HttpResponseStreamExtensions
	{
		private static readonly ILog Log = LogManager.GetLogger(typeof(HttpResponseStreamExtensions));
		//public static bool IsXsp;
		//public static bool IsModMono;
		public static bool IsMonoFastCgi;
		//public static bool IsWebDevServer;
		//public static bool IsIis;
		public static bool IsHttpListener;

		static HttpResponseStreamExtensions()
		{
			//IsXsp = Env.IsMono;
			//IsModMono = Env.IsMono;
			IsMonoFastCgi = Env.IsMono;

			//IsWebDevServer = !Env.IsMono;
			//IsIis = !Env.IsMono;
			IsHttpListener = HttpContext.Current == null;
		}
        
		public static void CloseOutputStream(this HttpResponse response)
		{
			try
			{
				//Don't close for MonoFastCGI as it outputs random 4-letters at the start
				if (!IsMonoFastCgi)
				{
					response.OutputStream.Flush();
					response.OutputStream.Close();
					//response.Close(); //This kills .NET Development Web Server
				}
			}
			catch (Exception ex)
			{
				Log.Error("Exception closing HttpResponse: " + ex.Message, ex);
			}
		}

		public static void CloseOutputStream(this HttpListenerResponse response)
		{
			try
			{
				response.OutputStream.Flush();
				response.OutputStream.Close();
				response.Close();
			}
			catch (Exception ex)
			{
				Log.Error("Error in HttpListenerResponseWrapper: " + ex.Message, ex);
			}
		}

	}
}