using System;
using System.IO;
using ServiceStack.Html;
using ServiceStack.Server;
using ServiceStack.ServiceHost;

namespace ServiceStack.Razor
{
    public abstract class ViewPage : ViewPageBase<dynamic>, IRazorView
	{
		public HtmlHelper Html = new HtmlHelper();

		public override Type ModelType
		{
			get { return typeof(DynamicRequestObject); }
		}

        public void Init(IViewEngine viewEngine, IHttpRequest httpReq, IHttpResponse httpRes)
        {
            base.Request = httpReq;
            base.Response = httpRes;

            Html.Init(viewEngine: viewEngine, httpReq: httpReq, httpRes: httpRes, razorPage:this);
        }

        public override void SetModel(object o)
        {
            base.SetModel(o);
            Html.SetModel(o);
        }

        public void WriteTo(StreamWriter writer)
        {
            this.Output = Html.Writer = writer;
            this.Execute();
            this.Output.Flush();
        }
	}

    public abstract class ViewPage<TModel> : ViewPageBase<TModel>, IRazorView
    {
        public HtmlHelper<TModel> Html = new HtmlHelper<TModel>();

        public int Counter { get; set; }

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

        public override void SetModel(object o)
        {
            base.SetModel(o);
            Html.SetModel(o);
        }

        public void WriteTo(StreamWriter writer)
        {
            this.Output = Html.Writer = writer;
            this.Execute();
            this.Output.Flush();
        }

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