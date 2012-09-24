using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface
{
    public class DefaultViewAttribute : RequestFilterAttribute
    {
        public string View { get; set; }
        public string Template { get; set; }

        public DefaultViewAttribute() { }
        public DefaultViewAttribute(string view) : this(view, null) { }
        public DefaultViewAttribute(string view, string template)
        {
            View = view;
            Template = template;
        }

        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            if (!string.IsNullOrEmpty(View))
                req.Items["View"] = View;
            if (!string.IsNullOrEmpty(Template))
                req.Items["Template"] = Template;
        }
    }
}