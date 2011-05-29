using ServiceStack.WebHost.EndPoints.Formats;

namespace ServiceStack.Markdown
{
	public abstract class MarkdownViewBase<T> : MarkdownViewBase
	{
		public new HtmlHelper<T> Html;

		protected MarkdownViewBase()
		{
			Html = (HtmlHelper<T>) GetHtmlHelper();
		}

		protected override HtmlHelper GetHtmlHelper()
		{
			return base.Html ?? new HtmlHelper<T>();
		}

		public override object Model
		{
			set
			{
				var typedModel = (T)value;
				Html.ViewData = new ViewDataDictionary<T>(typedModel);
				Html.ViewData.PopulateModelState();
			}
		}
	} 

	public abstract class MarkdownViewBase
	{
		public HtmlHelper Html;

		protected MarkdownViewBase()
		{
			Html = GetHtmlHelper();
		}

		protected virtual HtmlHelper GetHtmlHelper()
		{
			return Html ?? new HtmlHelper();
		}

		public virtual object Model
		{
			set
			{
				Html.ViewData = new ViewDataDictionary(value);
				Html.ViewData.PopulateModelState();
			}
		}

		public virtual MarkdownFormat Markdown
		{
			get
			{
				return Html.Markdown;
			}
			set
			{
				Html.Markdown = value;
			}
		}

		public virtual void InitHelpers()
		{
		}

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