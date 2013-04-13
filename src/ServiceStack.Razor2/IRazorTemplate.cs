using System;
using System.Collections.Generic;
using ServiceStack.Html;
using ServiceStack.Razor2.Templating;
using ServiceStack.ServiceHost;

namespace ServiceStack.Razor2
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