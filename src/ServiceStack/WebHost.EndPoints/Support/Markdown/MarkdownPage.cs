using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using ServiceStack.Common;
using ServiceStack.Markdown;
using ServiceStack.WebHost.EndPoints.Formats;

namespace ServiceStack.WebHost.EndPoints.Support.Markdown
{
	public class MarkdownPage : ITemplateWriter
	{
		public const string ModelName = "Model";

		public MarkdownPage()
		{
			this.Statements = new List<StatementExprBlock>();
			this.ExecutionContext = new EvaluatorExecutionContext();
		}

		public MarkdownPage(MarkdownFormat markdown, string fullPath, string name, string contents)
			: this()
		{
			Markdown = markdown;
			FilePath = fullPath;
			Name = name;
			Contents = contents;
		}

		public MarkdownFormat Markdown { get; set; }

		private int timesRun = 0;
		private bool hasCompletedFirstRun;

		public string FilePath { get; set; }
		public string Name { get; set; }
		public string Contents { get; set; }
		public string HtmlContents { get; set; }
		public EvaluatorExecutionContext ExecutionContext { get; private set; }

		private Evaluator evaluator;
		public Evaluator Evaluator
		{
			get
			{
				if (evaluator == null)
					throw new InvalidOperationException("evaluator not ready");

				return evaluator;
			}
		}

		private int exprSeq;

		public int GetNextId()
		{
			return exprSeq++;
		}

		public string GetTemplatePath()
		{
			var tplName = Path.Combine(
				Path.GetDirectoryName(this.FilePath),
				MarkdownFormat.TemplateName);

			return tplName;
		}

		public List<TemplateBlock> Blocks { get; set; }
		public List<StatementExprBlock> Statements { get; set; }

		public void Prepare()
		{
			if (!typeof(MarkdownViewBase).IsAssignableFrom(this.Markdown.MarkdownBaseType))
			{
				throw new ConfigurationErrorsException(
					"Config.MarkdownBaseType should derive from MarkdownViewBase");
			}
			
			if (this.Contents.IsNullOrEmpty()) return;

			this.Contents = StatementExprBlock.Extract(this.Contents, this.Statements);

			this.HtmlContents = Markdown.Transform(this.Contents);
			this.Blocks = this.HtmlContents.CreateTemplateBlocks(this.Statements);
		}

		public void Write(MarkdownViewBase instance, TextWriter textWriter, Dictionary<string, object> scopeArgs)
		{
			if (Interlocked.Increment(ref timesRun) == 1)
			{
				this.ExecutionContext.BaseType = Markdown.MarkdownBaseType;
				this.ExecutionContext.TypeProperties = Markdown.MarkdownGlobalHelpers;

				this.Blocks.ForEach(x => x.BeginFirstRun(this, scopeArgs));
				
				this.evaluator = this.ExecutionContext.Build();

				this.Blocks.ForEach(x => x.EndFirstRun(evaluator));
				
				hasCompletedFirstRun = true;
			}

			if (!hasCompletedFirstRun)
				throw new InvalidOperationException("Page hasn't finished initializing yet");

			if (this.evaluator != null)
			{
				instance = (MarkdownViewBase)(instance ?? this.evaluator.CreateInstance());
				instance.Markdown = Markdown;

				object model;
				if (scopeArgs.TryGetValue(ModelName, out model))
					instance.Model = model;
			}

			foreach (var block in Blocks)
			{
				block.Write(instance, textWriter, scopeArgs);
			}
		}
	}
}