using System;
using System.Collections.Generic;
using ServiceStack.Html;
using ServiceStack.Razor.Templating;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Razor
{
	public abstract class ViewPage<TModel> : TemplateBase<TModel>, IRazorTemplate
	{
		public UrlHelper Url = new UrlHelper();
		public HtmlHelper<TModel> Html = new HtmlHelper<TModel>();

        protected ViewPage()
        {
            this.ScopeArgs = new Dictionary<string, object>();
        }
        
        public IHttpRequest Request { get; set; }

        public IHttpResponse Response { get; set; }

        public string Layout { get; set; }

	    public Dictionary<string, object> ScopeArgs { get; set; }

        public Type ModelType
        {
            get { return typeof(TModel); }
        }

        private IAppHost appHost;
        public IAppHost AppHost
        {
            get { return appHost ?? EndpointHost.AppHost; }
            set { appHost = value; }
        }

        public T Get<T>()
        {
            return this.AppHost.TryResolve<T>();
        }

        public string Href(string url)
        {
            return Url.Content(url);
        }

        public void Prepend(string contents)
        {
            if (contents == null) return;
            Builder.Insert(0, contents);
        }

		public virtual void Init(IViewEngine viewEngine, ViewDataDictionary viewData, IHttpRequest httpReq, IHttpResponse httpRes)
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