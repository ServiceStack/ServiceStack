using System.Collections.Generic;
using System.IO;
using ServiceStack.Markdown;

namespace ServiceStack.Support.Markdown
{
	public interface ITemplateWriter
	{
		void Write(MarkdownViewBase instance, TextWriter textWriter, Dictionary<string, object> scopeArgs);
	}
}