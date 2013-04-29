using System;
using System.IO;
using ServiceStack.Html;
using ServiceStack.ServiceHost;

namespace ServiceStack.Razor
{
    public abstract class ViewPage : ViewPageBase<dynamic>, IRazorViewPage
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

        public void Init(IViewEngine viewEngine, IHttpRequest httpReq, IHttpResponse httpRes)
        {
            base.Request = httpReq;
            base.Response = httpRes;

            Html.Init(viewEngine: viewEngine, httpReq: httpReq, httpRes: httpRes, razorPage:this);
        }

        public void WriteTo(StreamWriter writer)
        {
            this.Output = Html.Writer = writer;
            this.Execute();
        }
	}

    public abstract class ViewPage<TModel> : ViewPageBase<TModel>, IRazorViewPage where TModel : class
    {
        public HtmlHelper<TModel> Html = new HtmlHelper<TModel>();

        public HtmlHelper HtmlHelper
        {
            get { return Html; }
        }

        public void Init(IViewEngine viewEngine, IHttpRequest httpReq, IHttpResponse httpRes)
        {
            base.Request = httpReq;
            base.Response = httpRes;

            Html.Init(viewEngine: viewEngine, httpReq: httpReq, httpRes: httpRes, razorPage: this);
        }

        public void WriteTo(StreamWriter writer)
        {
            this.Output = Html.Writer = writer;
            this.Execute();
        }

        //private IViewEngine viewEngine;
        //public override IViewEngine ViewEngine
        //{
        //    get { return viewEngine; }
        //    set
        //    {
        //        Html.ViewEngine = viewEngine = value;
        //    }
        //}

        public override Type ModelType
        {
            get { return typeof(TModel); }
        }

        //public override void Init(IRazorViewEngine viewEngine, ViewDataDictionary viewData, IHttpRequest httpReq, IHttpResponse httpRes)
        //{
        //    this.Request = httpReq;
        //    this.Response = httpRes;
        //    Html = new HtmlHelper<TModel>();
        //    Html.Init(httpReq, httpRes, viewEngine, viewData, null);
        //    if (viewData.Model is TModel)
        //        this.Model = (TModel)viewData.Model;
        //    else
        //        this.ModelError = viewData.Model;
        //}
    }
}