using System.Web;

namespace ServiceStack.WebHost.Endpoints.Extensions
{
	public static class HttpRequestExtensions
	{
		public static string GetOperationName(this HttpRequest request)
		{
			var pathInfo = request.GetPathInfo();
			if (string.IsNullOrEmpty(pathInfo)) return null;

			var operationName = pathInfo.Substring("/".Length);
			return operationName;
		}

		public static string GetPathInfo(this HttpRequest request)
		{
			var pathInfo = request.PathInfo;
			if (string.IsNullOrEmpty(pathInfo))
			{
				pathInfo = request.RawUrl.Substring(request.RawUrl.LastIndexOf("/"));
				if (pathInfo.IndexOf("?") != -1)
				{
					pathInfo = pathInfo.Substring(0, pathInfo.IndexOf("?"));
				}
			}
			return pathInfo;
		}
	}
}