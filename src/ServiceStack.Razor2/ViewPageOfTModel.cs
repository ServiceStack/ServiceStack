using System;
using ServiceStack.Html;

namespace ServiceStack.Razor2
{
    public abstract class ViewPage<TModel> : ViewPageBase<TModel> where TModel : class 
    {
        public HtmlHelper<TModel> Html = new HtmlHelper<TModel>();

        public HtmlHelper HtmlHelper
        {
            get { return Html; }
        }

        public void SetState(HtmlHelper htmlHelper)
        {
            if (htmlHelper == null) return;

            this.Request = htmlHelper.HttpRequest;
            this.Response = htmlHelper.HttpResponse;
            Html.SetState(htmlHelper);
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