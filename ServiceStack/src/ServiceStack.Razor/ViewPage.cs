using System;
using System.IO;
using ServiceStack.Html;
using ServiceStack.Web;

namespace ServiceStack.Razor;

public abstract class ViewPage : ViewPageBase<dynamic>, IRazorView
{
    public HtmlHelper Html = new();

    public dynamic Context { get; set; } //Unused, make Razor VS.NET Intelli-sense happy

    public override Type ModelType => typeof(DynamicRequestObject);

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
    public HtmlHelper<TModel> Html = new();

    public int Counter { get; set; }

    public HtmlHelper HtmlHelper => Html;

    public override StreamWriter Output
    {
        get => base.Output;
        set => base.Output = Html.Writer = value;
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

    public override Type ModelType => typeof(TModel);
}
