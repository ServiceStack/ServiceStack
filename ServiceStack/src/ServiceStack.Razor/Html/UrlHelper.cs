#if !NETCORE

using System.Web;

namespace ServiceStack.Html
{
	public class UrlHelper
	{
		public string Content(string url)
		{
		    return VirtualPathUtility.ToAbsolute(url);
		}
	}
}

#endif
