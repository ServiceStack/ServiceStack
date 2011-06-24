using System.Web;

namespace ServiceStack.Markdown
{
	public class UrlHelper
	{
		public string Content(string url)
		{
			return VirtualPathUtility.ToAbsolute(url);
		}
	}
}