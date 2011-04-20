using System.Collections.Generic;
using System.IO;
using ServiceStack.WebHost.EndPoints.Formats;

namespace ServiceStack.WebHost.EndPoints.Support.Markdown
{
	public class MarkdownTemplate : ITemplateWriter
	{
		public MarkdownTemplate() { }

		public MarkdownTemplate(string fullPath, string name, string contents)
		{
			FilePath = fullPath;
			Name = name;
			Contents = contents;
		}

		public string FilePath { get; set; }
		public string Name { get; set; }
		public string Contents { get; set; }

		public List<TemplateBlock> Blocks { get; set; }

		public void Prepare()
		{
			var blocks = this.Contents.SplitIntoBlocks(MarkdownFormat.TemplatePlaceHolder);
			this.Blocks = new List<TemplateBlock>();
			
			if (blocks.Count == 0) return;
			
			this.Blocks.Add(blocks[0]);
			if (blocks.Count == 1) return;
			
			this.Blocks.Add(new VariableBlock(TemplateBlock.ModelVarName));
			for (var i = 1; i < blocks.Count; i++)
			{
				this.Blocks.Add(blocks[i]);	
			}
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