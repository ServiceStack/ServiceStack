using System;
using System.IO;
using System.Web;
using System.Globalization;
using System.Collections.Generic;
using ServiceStack.Html;
using ServiceStack.MiniProfiler;
using ServiceStack.Razor.Templating;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Support.Markdown;

namespace ServiceStack.Razor
{
    public abstract class ViewPage<TModel> : ViewPageBase<TModel>
	{
		public HtmlHelper<TModel> Html = new HtmlHelper<TModel>();

        protected ViewPage()
        {
            this.ScopeArgs = new Dictionary<string, object>();
        }

        public override Type ModelType
        {
            get { return typeof(TModel); }
        }

		public override void Init(IRazorViewEngine viewEngine, ViewDataDictionary viewData, IHttpRequest httpReq, IHttpResponse httpRes)
		{
		    this.Request = httpReq;
			this.Response = httpRes;
			Html.Init(viewEngine, viewData);
		    this.Model = (TModel) viewData.Model;
		}

        public virtual bool IsSectionDefined(string sectionName)
        {
            //return this.childSections.ContainsKey(sectionName);
            return false;
        }
	}
}