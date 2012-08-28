using System;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Common;
using ServiceStack.Common.Net30;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.VirtualPath;

namespace ServiceStack.Html
{
    public class TemplateProvider
    {
        public bool CompileInParallel { get; set; }
        public int CompileWithNoOfThreads { get; set; }

        private static readonly ILog Log = LogManager.GetLogger(typeof(TemplateProvider));

        readonly string defaultTemplateName;

        public TemplateProvider(string defaultTemplateName)
        {
            this.defaultTemplateName = defaultTemplateName;
            this.CompileInParallel = false;
        }

        readonly Dictionary<string, IVirtualFile> templatePathsFound = new Dictionary<string, IVirtualFile>(StringComparer.InvariantCultureIgnoreCase);
        readonly HashSet<string> templatePathsNotFound = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        public string GetTemplatePath(IVirtualDirectory fileDir)
        {
            try
            {
                if (templatePathsNotFound.Contains(fileDir.VirtualPath)) return null;

                var templateDir = fileDir;
                IVirtualFile templateFile;
                while (templateDir != null && templateDir.GetFile(defaultTemplateName) == null)
                {
                    if (templatePathsFound.TryGetValue(templateDir.VirtualPath, out templateFile))
                        return templateFile.RealPath;

                    templateDir = templateDir.ParentDirectory;
                }

                if (templateDir != null)
                {
                    templateFile = templateDir.GetFile(defaultTemplateName);
                    templatePathsFound[templateDir.VirtualPath] = templateFile;
                    return templateFile.VirtualPath;
                }

                templatePathsNotFound.Add(fileDir.VirtualPath);
                return null;

            }
            catch (Exception ex)
            {
                ex.Message.Print();
                throw;
            }
        }

        private readonly ConcurrentQueue<IViewPage> compilePages = new ConcurrentQueue<IViewPage>();
        private readonly ConcurrentQueue<IViewPage> priorityCompilePages = new ConcurrentQueue<IViewPage>();

        public void QueuePageToCompile(IViewPage pageToCompile)
        {
            compilePages.Enqueue(pageToCompile);
        }

        //public void QueuePriorityPageToCompile(IViewPage pageToCompileNext)
        //{
        //    priorityCompilePages.Enqueue(pageToCompileNext);
        //    StartCompiling();
        //}

        private int runningThreads;
        public void CompileQueuedPages()
        {
            Log.InfoFormat("Starting to compile {0}/{1} pages, {2}",
                compilePages.Count, priorityCompilePages.Count,
                CompileInParallel ? "In Parallel" : "Sequentially");

            if (CompileInParallel)
            {
                var threadsToRun = Math.Min(CompileWithNoOfThreads, compilePages.Count);
                if (threadsToRun <= runningThreads) return;

                Log.InfoFormat("Starting {0} threads..", threadsToRun);

                threadsToRun.Times(x => {
                    ThreadPool.QueueUserWorkItem(waitHandle => CompileAllPages());
                });
            }
            else
            {
                CompileAllPages();
            }
        }

        private void CompileAllPages()
        {
            try
            {
                Interlocked.Increment(ref runningThreads);
                while (!compilePages.IsEmpty || !priorityCompilePages.IsEmpty)
                {
                    IViewPage viewPage;
                    //if (priorityCompilePages.TryDequeue(out viewPage))
                    //{
                    //    viewPage.Compile();
                    //}
                    if (compilePages.TryDequeue(out viewPage))
                    {
                        viewPage.Compile();
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref runningThreads);
                Log.InfoFormat("Compilation threads remaining {0}...", runningThreads);
            }
        }
    }

}

