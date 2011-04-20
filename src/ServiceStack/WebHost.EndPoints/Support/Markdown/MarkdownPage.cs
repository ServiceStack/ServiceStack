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
			FilePath = fullPath;
			Name = name;
			Contents = contents;
		}

		public string FilePath { get; set; }
		public string Name { get; set; }
		public string Contents { get; set; }
		public string HtmlContents { get; set; }

		public string GetTemplatePath()
		{
			var tplName = Path.Combine(
				Path.GetDirectoryName(this.FilePath),
				MarkdownFormat.TemplateName);

			return tplName;
		}

		public List<TemplateBlock> Blocks { get; set; }

		public void Prepare()
		{
			this.HtmlContents = MarkdownFormat.Instance.Transform(this.Contents);
			this.Blocks = this.HtmlContents.CreateTemplateBlocks();
		}

		public void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			foreach (var block in Blocks)
			{
				block.Write(textWriter, scopeArgs);
			}
		}
	}
}