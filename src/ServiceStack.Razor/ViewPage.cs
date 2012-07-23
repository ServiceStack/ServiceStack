using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Web;
using ServiceStack.Html;
using ServiceStack.MiniProfiler;
using ServiceStack.Razor.Templating;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Support.Markdown;

namespace ServiceStack.Razor
{
    public class ViewPage : ViewPageBase<DynamicRequestObject>
	{
		public RazorFormat RazorFormat { get; set; }

        public HtmlHelper Html = new HtmlHelper();

        public new dynamic Model { get; set; }

        public override Type ModelType
        {
            get { return typeof(DynamicRequestObject); }
        }

        public override void Init(IRazorViewEngine viewEngine, ViewDataDictionary viewData, IHttpRequest httpReq, IHttpResponse httpRes)
        {
            this.Request = httpReq;
            this.Response = httpRes;
            Html.Init(viewEngine, viewData);
            this.Model = new DynamicRequestObject(httpReq);
        }

        public virtual bool IsSectionDefined(string sectionName)
        {
            //return this.childSections.ContainsKey(sectionName);
            return false;
        }

        public string FilePath { get; set; }
		public string Name { get; set; }
		public string Contents { get; set; }

		public RazorPageType PageType { get; set; }
		public string TemplatePath { get; set; }
		public string DirectiveTemplatePath { get; set; }
		public DateTime? LastModified { get; set; }
		public List<IExpirable> Dependents { get; private set; }

		public const string ModelName = "Model";

		public ViewPage()
		{
			this.Dependents = new List<IExpirable>();
		}

		public ViewPage(RazorFormat razorFormat, string fullPath, string name, string contents)
			: this(razorFormat, fullPath, name, contents, RazorPageType.ViewPage) {}

		public ViewPage(RazorFormat razorFormat, string fullPath, string name, string contents, RazorPageType pageType)
			: this()
		{
			RazorFormat = razorFormat;
			FilePath = fullPath;
			Name = name;
			Contents = contents;
			PageType = pageType;
		}

        public DateTime? GetLastModified()
		{
			//if (!hasCompletedFirstRun) return null;
			var lastModified = this.LastModified;
			foreach (var expirable in this.Dependents)
			{
				if (!expirable.LastModified.HasValue) continue;
				if (!lastModified.HasValue || expirable.LastModified > lastModified)
				{
					lastModified = expirable.LastModified;
				}
			}
			return lastModified;
		}

		public string GetTemplatePath()
		{
			return this.DirectiveTemplatePath ?? this.TemplatePath;
		}

		public string PageName
		{
			get
			{
				return this.PageType == RazorPageType.Template
					|| this.PageType == RazorPageType.ContentPage
					? this.FilePath
					: this.Name;
			}
		}

		public void Prepare()
		{
			RazorHost.Compile(this.Contents, PageName);
		}

		private int timesRun;

		private Exception initException;
		readonly object readWriteLock = new object();
		private bool isBusy;
		public void Reload()
		{
			var contents = File.ReadAllText(this.FilePath);
			Reload(contents);
		}

		public void Reload(string contents)
		{
			var fi = new FileInfo(this.FilePath);
			var lastModified = fi.LastWriteTime;
			lock (readWriteLock)
			{
				try
				{
					isBusy = true;

					this.Contents = contents;
					foreach (var markdownReplaceToken in RazorFormat.ReplaceTokens)
					{
						this.Contents = this.Contents.Replace(markdownReplaceToken.Key, markdownReplaceToken.Value);
					}

					this.LastModified = lastModified;
					initException = null;
					timesRun = 0;
					Prepare();
				}
				catch (Exception ex)
				{
					initException = ex; 
				}
				isBusy = false;
				Monitor.PulseAll(readWriteLock);
			}
		}

		public string RenderToHtml()
		{
			return RenderToString((object)null);
		}

		public string RenderToHtml<T>(T model)
		{
			return RenderToString(model);
		}

		public string RenderToString<T>(T model)
		{
			var template = RazorFormat.ExecuteTemplate(model, this.PageName, this.TemplatePath);
			return template.Result;
		}

		public IRazorTemplate GetRazorTemplate()
		{
			return RazorHost.TemplateService.GetTemplate(this.PageName);
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

			var str = value as IHtmlString;

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