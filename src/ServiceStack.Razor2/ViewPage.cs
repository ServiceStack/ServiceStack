using System;
using System.IO;
using System.Web;
using System.Globalization;
using ServiceStack.Html;
using ServiceStack.MiniProfiler;
using ServiceStack.Razor.Templating;
using ServiceStack.ServiceHost;

namespace ServiceStack.Razor
{
	public abstract class ViewPage : ViewPageBase<DynamicRequestObject>
	{
		public HtmlHelper Html = new HtmlHelper();

        private IViewEngine viewEngine;
        public override IViewEngine ViewEngine
        {
            get { return viewEngine; }
            set
            {
                Html.ViewEngine = viewEngine = value;
            }
        }
		
		public new dynamic Model { get; set; }
		
		public override Type ModelType
		{
			get { return typeof(DynamicRequestObject); }
		}
		
		public override void Init(IRazorViewEngine viewEngine, ViewDataDictionary viewData, IHttpRequest httpReq, IHttpResponse httpRes)
		{
			this.Request = httpReq;
			this.Response = httpRes;
            Html.Init(httpReq, httpRes, viewEngine, viewData, null);
			this.Model = new DynamicRequestObject(httpReq);
		}
		
		public virtual bool IsSectionDefined(string sectionName)
		{
			//return this.childSections.ContainsKey(sectionName);
			return false;
		}
		
		public virtual void DefineSection(string sectionName, Action action)
		{
			//this.Sections.Add(sectionName, action);
		}
		
		private static string HtmlEncode(object value)
		{
			if (value == null)
			{
				return null;
			}
			
			var str = value as System.Web.IHtmlString;
			
			return str != null ? str.ToHtmlString() : HttpUtility.HtmlEncode(Convert.ToString(value, CultureInfo.CurrentCulture));
		}
		
		public virtual void WriteTo(TextWriter writer, HelperResult value)
		{
			if (value != null)
			{
				value.WriteTo(writer);
			}
		}
		
		public virtual void WriteLiteralTo(TextWriter writer, HelperResult value)
		{
			if (value != null)
			{
				value.WriteTo(writer);
			}
		}
	}
}