using System.Collections.Generic;
using System.IO;

namespace ServiceStack.WebHost.EndPoints.Support.Markdown
{
	public interface ITemplateWriter
	{
		void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs);
	}
}