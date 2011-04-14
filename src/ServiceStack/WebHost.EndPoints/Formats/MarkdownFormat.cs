using System.Web.UI;

namespace ServiceStack.WebHost.EndPoints.Formats
{
	public class MarkdownFormat
	{
		public static MarkdownFormat Instance = new MarkdownFormat();

		public void Init()
		{
			//Load all markdown templates and cache
		}
	}
}