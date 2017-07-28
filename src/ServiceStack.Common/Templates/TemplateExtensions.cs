using ServiceStack.Web;

namespace ServiceStack.Templates
{
    public static class TemplateExtensions
    {
        public static TemplateCodePage With(this TemplateCodePage page, IRequest request)
        {
            if (page is IRequiresRequest requiresRequest)
                requiresRequest.Request = request;
            return page;
        }
    }
}