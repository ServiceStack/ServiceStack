using System;
using ServiceStack.Web;

namespace ServiceStack
{
    /// <summary>
    /// Change the default HTML view or template used for the HTML response of this service
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
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

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            if (!string.IsNullOrEmpty(View))
            {
                object currentView;
                if (!req.Items.TryGetValue("View", out currentView) || string.IsNullOrEmpty(currentView as string))
                    req.Items["View"] = View;
            }
            if (!string.IsNullOrEmpty(Template))
            {
                object currentTemplate;
                if (!req.Items.TryGetValue("Template", out currentTemplate) || string.IsNullOrEmpty(currentTemplate as string))
                    req.Items["Template"] = Template;
            }
        }
    }
}