using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.WebHost.EndPoints.Formats;

namespace ServiceStack.WebHost.EndPoints.Support.Markdown
{
	public class MarkdownPage : ITemplateWriter
	{
		public MarkdownPage() { }

		public MarkdownPage(string fullPath, string name, string contents)
		{
			Path = fullPath;
			Name = name;
			Contents = contents;
		}

		public string Path { get; set; }
		public string Name { get; set; }
		public string Contents { get; set; }
		public string HtmlContents { get; set; }

		public string GetTemplatePath()
		{
			var tplName = System.IO.Path.Combine(
				System.IO.Path.GetDirectoryName(this.Path),
				MarkdownFormat.TemplateName);

			return tplName;
		}

		public List<TemplateBlock> Blocks { get; set; }

		public void Prepare()
		{
			this.HtmlContents = MarkdownFormat.Instance.Transform(this.Contents);
		}

		public void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			throw new NotImplementedException();
		}
	}
}