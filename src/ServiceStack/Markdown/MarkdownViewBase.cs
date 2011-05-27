using ServiceStack.WebHost.EndPoints.Formats;

namespace ServiceStack.Markdown
{
	public abstract class MarkdownViewBase<T> : MarkdownViewBase
	{		
	}

	public abstract class MarkdownViewBase
	{
		public static HtmlHelper Html = HtmlHelper.Instance;

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