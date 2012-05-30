using System;
using System.Collections.Generic;
using ServiceStack.Markdown;
using ServiceStack.RazorEngine.Templating;
using ServiceStack.ServiceHost;

namespace ServiceStack.RazorEngine
{
    public interface IRazorTemplate : ITemplate, ITemplatePage
    {
        void Init(IViewEngine viewEngine, ViewDataDictionary viewData, IHttpResponse httpRes);
        string Layout { get; }
        Dictionary<string, Action> Sections { get; }
        IRazorTemplate ChildTemplate { get; set; }
        void Prepend(string contents);
        IHttpResponse Response { get; }
    }
}