using System;
using System.Web;

namespace ServiceStack.WebHost.Endpoints.Support
{
	/// <summary>
	/// Highly optimized code to find if GZIP is supported from:
	///  - http://dotnetperls.com/gzip-request
	/// 
	/// Other resources for GZip, deflate resources:
	/// - http://www.west-wind.com/Weblog/posts/10564.aspx
	///		- http://www.west-wind.com/WebLog/posts/102969.aspx
	/// - ICSharpCode 
	/// </summary>
	public static class GzipOptimizationTest
	{
		public static HttpWorkerRequest GetWorker(HttpContext context)
		{
			var provider = (IServiceProvider)context;
			var worker = (HttpWorkerRequest)provider.GetService(typeof(HttpWorkerRequest));
			return worker;
		}

		public static bool HasSupport(HttpContext context)
		{
			try
			{
				var worker = GetWorker(context);
				string value = worker.GetKnownRequestHeader(HttpWorkerRequest.HeaderAcceptEncoding);
				if (value == null)
				{
					return false;
				}
				if (value.Length >= 4)
				{
					if (value[0] == 'g' && value[1] == 'z' && value[2] == 'i' && value[3] == 'p')
					{
						return true;
					}
					else
					{
						for (int i = 0; i < value.Length - 3; i++)
						{
							if ((value[i] == 'g' || value[i] == 'G') &&
								(value[i + 1] == 'z' || value[i + 1] == 'Z') &&
								(value[i + 2] == 'i' || value[i + 2] == 'I') &&
								(value[i + 3] == 'p' || value[i + 3] == 'P'))
							{
								return true;
							}
						}
					}
				}
				return false;
			}
			catch
			{
				return false;
			}
		}

		public static bool SlowWayToCheckIfGZipIsSupported(HttpRequest request)
		{
			//using request.Headers[] builds the ASP.NET NameValueCollection which takes time
			string encoding = request.Headers["Accept-Encoding"];
			if (!string.IsNullOrEmpty(encoding) && encoding.Contains("gzip"))
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}