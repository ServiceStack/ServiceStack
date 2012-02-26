using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using ServiceStack.Common;
using ServiceStack.Markdown;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Formats;

namespace ServiceStack.WebHost.Endpoints.Support.Markdown
{
	public class MarkdownPage : IExpirable
	{
		public const string ModelName = "Model";

		public MarkdownPage()
		{
			this.ExecutionContext = new EvaluatorExecutionContext();
			this.Dependents = new List<IExpirable>();
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

		public MarkdownFormat Markdown { get; set; }

		private int timesRun;
		private bool hasCompletedFirstRun;

		public MarkdownPageType PageType { get; set; }
		public string FilePath { get; set; }
		public string Name { get; set; }
		public string Contents { get; set; }
		public string HtmlContents { get; set; }
		public string TemplatePath { get; set; }
		public string DirectiveTemplatePath { get; set; }
		public EvaluatorExecutionContext ExecutionContext { get; private set; }

		public DateTime? LastModified { get; set; }
		public List<IExpirable> Dependents { get; private set; }

		public DateTime? GetLastModified()
		{
			if (!hasCompletedFirstRun) return null;
			var lastModified = this.LastModified;
			foreach (var expirable in this.Dependents)
			{
				if (!expirable.LastModified.HasValue) continue;
				if (!lastModified.HasValue || expirable.LastModified > lastModified)
				{
					lastModified = expirable.LastModified;
				}
			}
			return lastModified;
		}

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

		public TemplateBlock[] MarkdownBlocks { get; set; }
		public TemplateBlock[] HtmlBlocks { get; set; }

		private Exception initException;
		readonly object readWriteLock = new object();
		private bool isBusy;
		public void Reload()
		{
			var fi = new FileInfo(this.FilePath);
			var lastModified = fi.LastWriteTime;
			var contents = File.ReadAllText(this.FilePath);

			lock (readWriteLock)
			{
				try
				{
					isBusy = true;

					this.Contents = contents;
					foreach (var markdownReplaceToken in Markdown.MarkdownReplaceTokens)
					{
						this.Contents = this.Contents.Replace(markdownReplaceToken.Key, markdownReplaceToken.Value);
					}
					
					this.LastModified = lastModified;
					initException = null;
					exprSeq = 0;
					timesRun = 0;
					ExecutionContext = new EvaluatorExecutionContext();
					Prepare();
				}
				catch (Exception ex)
				{
					initException = ex;
				}				
				isBusy = false;
				Monitor.PulseAll(readWriteLock);
			}
		}

		public void Prepare()
		{
			if (!typeof(MarkdownViewBase).IsAssignableFrom(this.Markdown.MarkdownBaseType))
			{
				throw new ConfigurationErrorsException(
					"Config.MarkdownBaseType must inherit from MarkdownViewBase");
			}

			if (this.Contents.IsNullOrEmpty()) return;

			foreach (var markdownReplaceToken in Markdown.MarkdownReplaceTokens)
			{
				this.Contents = this.Contents.Replace(markdownReplaceToken.Key, markdownReplaceToken.Value);
			}

			var markdownStatements = new List<StatementExprBlock>();

			var markdownContents = StatementExprBlock.Extract(this.Contents, markdownStatements);

			this.MarkdownBlocks = markdownContents.CreateTemplateBlocks(markdownStatements).ToArray();

			var htmlStatements = new List<StatementExprBlock>();
			var htmlContents = StatementExprBlock.Extract(this.Contents, htmlStatements);

			this.HtmlContents = Markdown.Transform(htmlContents);
			this.HtmlBlocks = this.HtmlContents.CreateTemplateBlocks(htmlStatements).ToArray();

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
				pageContext = new PageContext(this, new Dictionary<string, object>(), true);

			var blocks = pageContext.RenderHtml ? this.HtmlBlocks : this.MarkdownBlocks;

			if (Interlocked.Increment(ref timesRun) == 1)
			{
				lock (readWriteLock)
				{
					try
					{
						isBusy = true;

						this.ExecutionContext.BaseType = Markdown.MarkdownBaseType;
						this.ExecutionContext.TypeProperties = Markdown.MarkdownGlobalHelpers;

						pageContext.MarkdownPage = this;
						var initHtmlContext = pageContext.Create(this, true);
						var initMarkdownContext = pageContext.Create(this, false);

						foreach (var block in this.HtmlBlocks) block.DoFirstRun(initHtmlContext);
						foreach (var block in this.MarkdownBlocks) block.DoFirstRun(initMarkdownContext);

						this.evaluator = this.ExecutionContext.Build();

						foreach (var block in this.HtmlBlocks) block.AfterFirstRun(evaluator);
						foreach (var block in this.MarkdownBlocks) block.AfterFirstRun(evaluator);

						AddDependentPages(blocks);

						initException = null;
						hasCompletedFirstRun = true;
					}
					catch (Exception ex)
					{
						initException = ex;
						throw;
					}
					finally
					{
						isBusy = false;
					}
				}
			}

			lock (readWriteLock)
			{
				while (isBusy)
					Monitor.Wait(readWriteLock);
			}

			if (initException != null)
			{
				timesRun = 0;
				throw initException;
			}

			MarkdownViewBase instance = null;
			if (this.evaluator != null)
			{
				instance = (MarkdownViewBase)this.evaluator.CreateInstance();

				object model;
				pageContext.ScopeArgs.TryGetValue(ModelName, out model);

				instance.Init(Markdown.AppHost, this, pageContext.ScopeArgs, model, pageContext.RenderHtml);
			}

			foreach (var block in blocks)
			{
				block.Write(instance, textWriter, pageContext.ScopeArgs);
			}

			if (instance != null)
			{
				instance.OnLoad();
			}
		}

		private void AddDependentPages(IEnumerable<TemplateBlock> blocks)
		{
			foreach (var block in blocks)
			{
				var exprBlock = block as MethodStatementExprBlock;
				if (exprBlock == null || exprBlock.DependentPageName == null) continue;
				var page = Markdown.GetViewPage(exprBlock.DependentPageName);
				if (page != null)
					Dependents.Add(page);
			}

			MarkdownTemplate template;
			if (this.DirectiveTemplatePath != null
				&& Markdown.PageTemplates.TryGetValue(this.DirectiveTemplatePath, out template))
			{
				this.Dependents.Add(template);
			}
			if (this.TemplatePath != null
				&& Markdown.PageTemplates.TryGetValue(this.TemplatePath, out template))
			{
				this.Dependents.Add(template);
			}

		}
	}
}