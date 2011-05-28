using System;
using ServiceStack.Markdown.Html;

namespace ServiceStack.Markdown
{
	public abstract class MarkdownViewBase<T> : MarkdownViewBase
	{
		public new HtmlHelper<T> Html;

		protected MarkdownViewBase()
		{
			Html = new HtmlHelper<T>();
		}

		public override object Model
		{
			set
			{
				var typedModel = (T)value;
				Html.ViewData = new ViewDataDictionary<T>(typedModel);
				((HtmlHelper)Html).ViewData = Html.ViewData;
			}
		}
	}

	public abstract class MarkdownViewBase
	{
		public HtmlHelper Html;

		protected MarkdownViewBase()
		{
			Html = HtmlHelper.Instance;
		}

		public virtual object Model
		{
			set
			{
				Html.ViewData = new ViewDataDictionary(value);
			}
		}

		public virtual void InitHelpers()
		{
		}

		public string Raw(string content)
		{
			return Html.Raw(content);
		}

		public string Partial(string viewName, object model)
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