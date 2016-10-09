using ServiceStack.Text;

namespace ServiceStack.Html
{
    public static class HtmlExtensions
    {
        public static MvcHtmlString AsRawJson<T>(this T model)
        {
            var json = !Equals(model, default(T)) ? model.ToJson() : "null";
            return MvcHtmlString.Create(json);
        }

        public static MvcHtmlString AsRaw<T>(this T model)
        {
            return MvcHtmlString.Create(
                (model != null ? model : default(T))?.ToString());
        }
    }
}