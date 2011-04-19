using System.Collections.Generic;
using System.IO;
using System.Text;
using ServiceStack.Common;

namespace ServiceStack.WebHost.EndPoints.Support.Markdown
{
	public static class TemplateExtensions
	{
		public static string RenderToString(this ITemplateWriter templateWriter, Dictionary<string, object> scopeArgs)
		{
			var sb = new StringBuilder();
			using (var writer = new StringWriter(sb))
			{
				templateWriter.Write(writer, scopeArgs);
			}
			return sb.ToString();
		}

		public static string RenderToString(this IEnumerable<ITemplateWriter> templateWriters, Dictionary<string, object> scopeArgs)
		{
			var sb = new StringBuilder();
			using (var writer = new StringWriter(sb))
			{
				foreach (var templateWriter in templateWriters)
				{
					templateWriter.Write(writer, scopeArgs);
				}
			}
			return sb.ToString();
		}

		public static List<TemplateBlock> SplitIntoBlocks(this string content, string onPlaceHolder)
		{
			var blocks = new List<TemplateBlock>();
			if (content.IsNullOrEmpty()) return blocks;

			var pos = 0;
			var lastPos = 0;
			while ((pos = content.IndexOf(onPlaceHolder, lastPos)) != -1)
			{
				var contentBlock = content.Substring(lastPos, pos - lastPos);

				blocks.Add(new TextBlock(contentBlock));

				lastPos = pos + onPlaceHolder.Length;
			}

			if (lastPos != content.Length - 1)
			{
				var lastBlock = content.Substring(lastPos);
				blocks.Add(new TextBlock(lastBlock));
			}

			return blocks;
		}
	}
}