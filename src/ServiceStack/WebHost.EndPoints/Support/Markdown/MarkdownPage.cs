using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using ServiceStack.Common;
using ServiceStack.Markdown;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.EndPoints.Formats;

namespace ServiceStack.WebHost.EndPoints.Support.Markdown
{
	public class MarkdownPage
	{
		public const string ModelName = "Model";

		public MarkdownPage()
		{
			this.Statements = new List<StatementExprBlock>();
			this.ExecutionContext = new EvaluatorExecutionContext();
			this.RenderHtml = true;
		}

		public MarkdownPage(MarkdownFormat markdown, string fullPath, string name, string contents)
			: this(markdown, fullPath, name, contents, MarkdownPageType.ViewPage)
		{
		}

		public MarkdownPage(MarkdownFormat markdown, string fullPath, string name, string contents, MarkdownPageType pageType)
			: this()
		{
			Markdown = markdown;
			FilePath = fullPath;
			Name = name;
			Contents = contents;
			PageType = pageType;
		}

		public MarkdownPage(MarkdownFormat markdown, string fullPath, string name, string contents, bool renderHtml)
			: this(markdown, fullPath, name, contents, MarkdownPageType.ViewPage)
		{
			this.RenderHtml = renderHtml;
		}

		public MarkdownFormat Markdown { get; set; }

		private int timesRun;
		private bool hasCompletedFirstRun;

		public MarkdownPageType PageType { get; set; }
		public string FilePath { get; set; }
		public string Name { get; set; }
		public string Contents { get; set; }
		public string HtmlContents { get; set; }
		public bool RenderHtml { get; set; }
		public string TemplatePath { get; set; }
		public string DirectiveTemplatePath { get; set; }
		public DateTime? LastModified { get; set; }
		public EvaluatorExecutionContext ExecutionContext { get; private set; }

		public string GetTemplatePath()
		{
			return this.DirectiveTemplatePath ?? this.TemplatePath;
		}

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

		public List<TemplateBlock> MarkdownBlocks { get; set; }
		public List<TemplateBlock> HtmlBlocks { get; set; }
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

			this.MarkdownBlocks = this.Contents.CreateTemplateBlocks(this.Statements);

			this.HtmlContents = Markdown.Transform(this.Contents);
			this.HtmlBlocks = this.HtmlContents.CreateTemplateBlocks(this.Statements);

			SetTemplateDirectivePath();
		}

		private void SetTemplateDirectivePath()
		{
			var templateDirective = this.HtmlBlocks.FirstOrDefault(
				x => x is DirectiveBlock && ((DirectiveBlock)x).TemplatePath != null);
			if (templateDirective == null) return;

			var fileDir = Path.GetDirectoryName(this.FilePath);
			var templatePath = ((DirectiveBlock)templateDirective).TemplatePath;
			if (templatePath.StartsWith("~"))
			{
				this.DirectiveTemplatePath = Path.GetFullPath(templatePath.ReplaceFirst("~", fileDir));
			}
			else
			{
				if (templatePath.IsRelativePath())
				{
					this.DirectiveTemplatePath = Path.GetFullPath(Path.Combine(fileDir, templatePath));
				}
			}
		}

		public void Write(TextWriter textWriter, PageContext pageContext)
		{
			if (textWriter == null)
				throw new ArgumentNullException("textWriter");

			if (pageContext == null)
				pageContext = new PageContext(this, new Dictionary<string, object>(), this.RenderHtml);


			var blocks = pageContext.RenderHtml ? this.HtmlBlocks : this.MarkdownBlocks;

			if (Interlocked.Increment(ref timesRun) == 1)
			{
				this.ExecutionContext.BaseType = Markdown.MarkdownBaseType;
				this.ExecutionContext.TypeProperties = Markdown.MarkdownGlobalHelpers;

				pageContext.MarkdownPage = this;
				blocks.ForEach(x => x.DoFirstRun(pageContext));

				this.evaluator = this.ExecutionContext.Build();

				blocks.ForEach(x => x.AfterFirstRun(evaluator));

				hasCompletedFirstRun = true;
			}

			if (!hasCompletedFirstRun) //TODO: Add lock/waits if it's a noticeable problem
				throw new InvalidOperationException("Page hasn't finished initializing yet");

			MarkdownViewBase instance = null;
			if (this.evaluator != null)
			{
				instance = (MarkdownViewBase)this.evaluator.CreateInstance();

				object model;
				pageContext.ScopeArgs.TryGetValue(ModelName, out model);

				instance.Init(this, model, this.RenderHtml);
			}

			foreach (var block in blocks)
			{
				block.Write(instance, textWriter, pageContext.ScopeArgs);
			}
		}
	}
}