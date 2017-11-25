using System.Web;

namespace ServiceStack.Html
{
    // Dummy class to satisfy linked files from SS.Razor project
    public class HelperResult : IHtmlString
    {
        public string ToHtmlString()
        {
            throw new System.NotImplementedException();
        }
    }
}
