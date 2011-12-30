using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ServiceStack.WebHost.EndPoints.Support.Markdown;

namespace ServiceStack.RazorEngine
{
	public class RazorPage
	{
		public RazorFormat RazorFormat { get; set; }

		public string FilePath { get; set; }
		public string Name { get; set; }
		public string Contents { get; set; }

		public RazorPageType PageType { get; set; }
		public string TemplatePath { get; set; }
		public string DirectiveTemplatePath { get; set; }
		public DateTime? LastModified { get; set; }
		public List<IExpirable> Dependents { get; private set; }

		public const string ModelName = "Model";

		public RazorPage()
		{
			this.Dependents = new List<IExpirable>();
		}

		public RazorPage(RazorFormat razorFormat, string fullPath, string name, string contents)
			: this(razorFormat, fullPath, name, contents, RazorPageType.ViewPage)
		{
		}

		public RazorPage(RazorFormat razorFormat, string fullPath, string name, string contents, RazorPageType pageType)
			: this()
		{
			RazorFormat = razorFormat;
			FilePath = fullPath;
			Name = name;
			Contents = contents;
			PageType = pageType;
		}

		public DateTime? GetLastModified()
		{
			//if (!hasCompletedFirstRun) return null;
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

		public string PageName
		{
			get
			{
				return this.PageType == RazorPageType.Template
					|| this.PageType == RazorPageType.ContentPage
					? this.FilePath
					: this.Name;
			}
		}

		public void Prepare()
		{
			Razor.Compile(this.Contents, PageName);
		}

		private int timesRun;

		private Exception initException;
		readonly object readWriteLock = new object();
		private bool isBusy;
		public void Reload()
		{
			var contents = File.ReadAllText(this.FilePath);
			Reload(contents);
		}

		public void Reload(string contents)
		{
			var fi = new FileInfo(this.FilePath);
			var lastModified = fi.LastWriteTime;
			lock (readWriteLock)
			{
				try
				{
					isBusy = true;

					this.Contents = contents;
					foreach (var markdownReplaceToken in RazorFormat.MarkdownReplaceTokens)
					{
						this.Contents = this.Contents.Replace(markdownReplaceToken.Key, markdownReplaceToken.Value);
					}

					this.LastModified = lastModified;
					initException = null;
					timesRun = 0;
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

		public string RenderToHtml()
		{
			return RenderToString((object)null);
		}

		public string RenderToHtml<T>(T model)
		{
			return RenderToString(model);
		}

		public string RenderToString<T>(T model)
		{
			var template = RazorFormat.ExecuteTemplate(model, this.PageName, this.TemplatePath);
			return template.Result;
		}

		public IRazorTemplate GetRazorTemplate()
		{
			return Razor.DefaultTemplateService.GetTemplate(this.PageName);
		}
	}
}