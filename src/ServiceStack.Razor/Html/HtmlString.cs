using System;
using System.Globalization;
using System.IO;

namespace ServiceStack.Html
{
	public class HelperResult : IHtmlString, System.Web.IHtmlString
	{
		private readonly Action<TextWriter> action;

		public HelperResult(Action<TextWriter> action)
		{
			this.action = action ?? throw new ArgumentNullException(nameof(action), "The action parameter cannot be null.");
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