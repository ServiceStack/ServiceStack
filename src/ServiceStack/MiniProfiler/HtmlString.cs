using System;
using System.Globalization;
using System.IO;

namespace ServiceStack.MiniProfiler
{
	public interface IHtmlString
	{
		string ToHtmlString();
	}

    public class HtmlString : IHtmlString, System.Web.IHtmlString
	{
		private string value;

		public HtmlString(string value)
		{
			this.value = value;
		}

		public override string ToString()
		{
			return value;
		}

		public string ToHtmlString()
		{
			return this.ToString();
		}
	}

	public class HelperResult : IHtmlString, System.Web.IHtmlString
	{
		private readonly Action<TextWriter> action;

		public HelperResult(Action<TextWriter> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action", "The action parameter cannot be null.");
			}

			this.action = action;
		}

		public string ToHtmlString()
		{
			return this.ToString();
		}

		public override string ToString()
		{
			using (var stringWriter = new StringWriter(CultureInfo.InvariantCulture))
			{
				this.action(stringWriter);
				return stringWriter.ToString();
			}
		}

		public void WriteTo(TextWriter writer)
		{
			this.action(writer);
		}
	}
}