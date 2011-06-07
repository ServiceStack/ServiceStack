using System.Collections.Generic;
using ServiceStack.WebHost.EndPoints.Support.Markdown;

namespace ServiceStack.Markdown
{
	public abstract class MarkdownViewBase<T> : MarkdownViewBase
	{
		private HtmlHelper<T> html;
		public new HtmlHelper<T> Html
		{
			get { return html ?? (html = (HtmlHelper<T>) base.Html); }
		}

		protected override HtmlHelper GetHtmlHelper()
		{
			return base.Html ?? new HtmlHelper<T>();
		}

		public override void Init(MarkdownPage markdownPage, Dictionary<string, object> scopeArgs, object model, bool renderHtml)
		{
			this.RenderHtml = renderHtml;
			this.ScopeArgs = scopeArgs;
			this.MarkdownPage = markdownPage;

			var typedModel = (T)model;
			Html.Init(markdownPage, scopeArgs, renderHtml, new ViewDataDictionary<T>(typedModel));

			InitHelpers();
		}
	}  

	public abstract class MarkdownViewBase
	{
		public MarkdownPage MarkdownPage { get; protected set; }
		public HtmlHelper Html { get; protected set; }
		public Dictionary<string,object> ScopeArgs { get; protected set; }
		public bool RenderHtml { get; protected set; }
		public object Model { get; protected set; }

		protected MarkdownViewBase()
		{ 
			Html = GetHtmlHelper();
		}

		/// <summary>
		/// Ensure the same instance is used for subclasses
		/// </summary>
		protected virtual HtmlHelper GetHtmlHelper()
		{
			return Html ?? new HtmlHelper();
		}

		public virtual void Init(MarkdownPage markdownPage, Dictionary<string,object> scopeArgs, object model, bool renderHtml)
		{
			this.RenderHtml = renderHtml;
			this.ScopeArgs = scopeArgs;
			this.MarkdownPage = markdownPage;

			Html.Init(markdownPage, scopeArgs, renderHtml, new ViewDataDictionary(model));

			InitHelpers();
		}

		public virtual void InitHelpers() {}

		public virtual void OnLoad() { }

		public string Raw(string content)
		{
			return Html.Raw(content);
		}

		public MvcHtmlString Partial(string viewName, object model)
		{
			return Html.Partial(viewName, model);
		}

		public string Lower(string name)
		{
			return name == null ? null : name.ToLower();
		}

		public string Upper(string name)
		{
			return name == null ? null : name.ToUpper();
		}

		public string Combine(string separator, params string[] parts)
		{
			return string.Join(separator, parts);
		}
	}
}