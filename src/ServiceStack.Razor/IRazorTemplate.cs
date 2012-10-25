using System;
using System.Collections.Generic;
using ServiceStack.Html;
using ServiceStack.Razor.Templating;
using ServiceStack.ServiceHost;

namespace ServiceStack.Razor
{
	public interface IRazorTemplate : ITemplate, ITemplatePage
	{
	    void Init(IRazorViewEngine viewEngine, ViewDataDictionary viewData, IHttpRequest httpReq, IHttpResponse httpRes);
		string Layout { get; }
		Dictionary<string, Action> Sections { get; }
		IRazorTemplate ChildTemplate { get; set; }
		void Prepend(string contents);
		IHttpResponse Response { get; }
	    Type ModelType { get; }
	}
}