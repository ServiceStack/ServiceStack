using System;
using ServiceStack.Web;

namespace ServiceStack;

/// <summary>
/// Change the default HTML view or template used for the HTML response of this service
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class DefaultViewAttribute : RequestFilterAttribute
{
    public string View { get; set; }
    public string Template { get; set; }

    public DefaultViewAttribute() : this(null,null) { }
    public DefaultViewAttribute(string view) : this(view, null) { }
    public DefaultViewAttribute(string view, string template)
    {
        View = view;
        Template = template;
        Priority = -1;
    }

    public override void Execute(IRequest req, IResponse res, object requestDto)
    {
        if (!string.IsNullOrEmpty(View))
        {
            if (!req.Items.TryGetValue(Keywords.View, out var currentView) || string.IsNullOrEmpty(currentView as string))
                req.SetView(View);
        }
        if (!string.IsNullOrEmpty(Template))
        {
            if (!req.Items.TryGetValue(Keywords.Template, out var currentTemplate) || string.IsNullOrEmpty(currentTemplate as string))
                req.SetTemplate(Template);
        }
    }
}