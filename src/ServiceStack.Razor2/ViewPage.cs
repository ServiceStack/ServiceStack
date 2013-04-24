using System;
using ServiceStack.Html;

namespace ServiceStack.Razor2
{
	public abstract class ViewPage : ViewPageBase<DynamicRequestObject>
	{
		public HtmlHelper Html = new HtmlHelper();

        //private IViewEngine viewEngine;
        //public override IViewEngine ViewEngine
        //{
        //    get { return viewEngine; }
        //    set
        //    {
        //        Html.ViewEngine = viewEngine = value;
        //    }
        //}
		
        //public new dynamic Model { get; set; }
		
		public override Type ModelType
		{
			get { return typeof(DynamicRequestObject); }
		}
		
        //public override void Init(IRazorViewEngine viewEngine, ViewDataDictionary viewData, IHttpRequest httpReq, IHttpResponse httpRes)
        //{
        //    this.Request = httpReq;
        //    this.Response = httpRes;
        //    Html.Init(httpReq, httpRes, viewEngine, viewData, null);
        //    this.Model = new DynamicRequestObject(httpReq);
        //}
	}
}