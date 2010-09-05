using System.Net;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	public static class SupportExtensions
	{
		public static string GetOperationName(this HttpListenerRequest request)
		{
			return request.Url.Segments[request.Url.Segments.Length - 1];
		}		
	}
}