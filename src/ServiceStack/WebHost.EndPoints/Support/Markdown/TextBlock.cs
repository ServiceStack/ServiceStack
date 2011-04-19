using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Text;

namespace ServiceStack.WebHost.EndPoints.Support.Markdown
{
	public class TextBlock : TemplateBlock
	{
		public TextBlock(string content)
		{
			Content = content;
		}

		public string Content { get; set; }
		
		public override void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			textWriter.Write(Content);
		}
	}

	public class VariableBlock : TemplateBlock
	{
		public string varName;

		public VariableBlock(string varName)
		{
			this.varName = varName;
		}

		public override void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			object value = null;
			scopeArgs.TryGetValue(varName, out value);
			
			if (value == null)
				return;

			textWriter.Write(value);
		}
	}

	public abstract class TemplateBlock : ITemplateWriter
	{
		public const string ModelVarName = "model";

		public abstract void Write(TextWriter textWriter, Dictionary<string, object> scopeArgs);
	}
}