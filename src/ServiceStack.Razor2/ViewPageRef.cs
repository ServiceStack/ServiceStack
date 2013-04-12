using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using ServiceStack.Razor2.Templating;
using ServiceStack.Text;
using ServiceStack.Logging;
using ServiceStack.WebHost.Endpoints.Support.Markdown;

namespace ServiceStack.Razor2
{
    public class ViewPageRef : IViewPage
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof (ViewPageRef));

		public TemplateService Service { get; set; }
		
		public RazorFormat RazorFormat { get; set; }

        public string FilePath { get; set; }
		public string Name { get; set; }
		public string Contents { get; set; }

		public RazorPageType PageType { get; set; }
		public string Template { get; set; }
		public DateTime? LastModified { get; set; }
		public List<IExpirable> Dependents { get; private set; }

		public const string ModelName = "Model";

		public ViewPageRef()
		{
			this.Dependents = new List<IExpirable>();
		}

		public ViewPageRef(RazorFormat razorFormat, string fullPath, string name, string contents)
			: this(razorFormat, fullPath, name, contents, RazorPageType.ViewPage) {}

		public ViewPageRef(RazorFormat razorFormat, string fullPath, string name, string contents, RazorPageType pageType)
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

        bool isCompiled;
        public bool IsCompiled
        {
            get { return isCompiled; }
        }

        public void Compile(bool force=false)
		{
            if (IsCompiled && !force) return;
            lock (this)
            {
                if (IsCompiled && !force) return;
                var sw = Stopwatch.StartNew();
                try
                {
                    Service.Compile(this, this.Contents, PageName);
                    Log.InfoFormat("Compiled {0} in {1}ms", this.FilePath, sw.ElapsedMilliseconds);
                }
                catch (TemplateCompilationException tcex)
                {
                    var errors = new StringBuilder();
                    foreach (var error in tcex.Errors)
                        errors.AppendLine(" -- {0}".Fmt(error));
                    Log.Error("Error compiling {0} with errors:{1}{2}".Fmt(this.FilePath, Environment.NewLine, errors), tcex);
                    throw;
                }
                catch (Exception ex)
                {                    
                    Log.Error("Error compiling {0}".Fmt(this.FilePath), ex);
                    throw;
                } 
                isCompiled = true;
            }
		}

        public void EnsureCompiled()
        {
            Compile();
        }
        
		private int timesRun;

		private Exception initException;
		readonly object readWriteLock = new object();
		private bool isBusy;
        
		public void Reload(string contents, DateTime lastModified)
		{
			lock (readWriteLock)
			{
				try
				{
					isBusy = true;

					this.Contents = contents;
					this.LastModified = lastModified;
					initException = null;
					timesRun = 0;
					Compile(force:true);
				}
				catch (Exception ex)
				{
					initException = ex; 
				}
				isBusy = false;
				Monitor.PulseAll(readWriteLock);
			}
		}

		public IRazorTemplate GetRazorTemplate()
		{
			return Service.GetTemplate(this.PageName);
		}


        //DS Commenting out to simplify 'Find all references' as these seem to be called only from unit tests in razor v1
        //DS Should these be permanently removed? 
        #region Unit test support
        //public string RenderToHtml()
        //{
        //    return RenderToString((object)null);
        //}
		
        //public string RenderToHtml<T>(T model)
        //{
        //    return RenderToString(model);
        //}
		
        //public string RenderToString<T>(T model)
        //{
        //    var template = ExecuteTemplate(model);
        //    return template.Result;
        //}

        //public IRazorTemplate ExecuteTemplate<T>(T model)
        //{
        //    return RazorFormat.ExecuteTemplate(model, this.PageName, this.Template);
        //}

        //public IRazorTemplate InitTemplate<T>(T model)
        //{
        //    var templateService = RazorFormat.GetTemplateService(PageName);
        //    var template = templateService.GetTemplate(PageName);
        //    templateService.InitTemplate(model, template);
        //    return template;
        //}
        #endregion
    }
}