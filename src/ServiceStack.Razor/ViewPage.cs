using System;
using System.IO;
using ServiceStack.Html;
using ServiceStack.Web;

namespace ServiceStack.Razor
{
    public abstract class ViewPage : ViewPageBase<dynamic>, IRazorView
    {
        public HtmlHelper Html = new HtmlHelper();

        public dynamic Context { get; set; } //Unused, make Razor VS.NET Intelli-sense happy

        public override Type ModelType
        {
            get { return typeof(DynamicRequestObject); }
        }

        public void Init(IViewEngine viewEngine, IRequest httpReq, IResponse httpRes)
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
    }

    public abstract class ViewPage<TModel> : ViewPageBase<TModel>, IRazorView
    {
        public HtmlHelper<TModel> Html = new HtmlHelper<TModel>();

        public int Counter { get; set; }

        public HtmlHelper HtmlHelper
        {
            get { return Html; }
        }

        public override StreamWriter Output
        {
            get
            {
                return base.Output;
            }
            set
            {
                base.Output = Html.Writer = value;
            }
        }

        public void Init(IViewEngine viewEngine, IRequest httpReq, IResponse httpRes)
        {
            base.Request = httpReq;
            base.Response = httpRes;

            Html.Init(viewEngine: viewEngine, httpReq: httpReq, httpRes: httpRes, razorPage: this);
        }

        public override void SetModel(object o)
        {
            base.SetModel(o);
            if (o is TModel)
            {
                Html.SetModel(o);
            }
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
    }
}