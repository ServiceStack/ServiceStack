using ServiceStack.Text;

namespace ServiceStack.Html
{
    public static class HtmlExtensions
    {
         public static MvcHtmlString ToRawJson<T>(this T model)
         {
             return MvcHtmlString.Create(model.ToJson());
         }
    }
}