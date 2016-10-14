using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Common;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Html
{
    public class TemplateProvider
    {
        public int? CompileInParallelWithNoOfThreads { get; set; }

        private static readonly ILog Log = LogManager.GetLogger(typeof(TemplateProvider));

        AutoResetEvent waiter = new AutoResetEvent(false);

        readonly string defaultTemplateName;

        public TemplateProvider(string defaultTemplateName)
        {
            this.defaultTemplateName = defaultTemplateName;
        }

        readonly Dictionary<string, IVirtualFile> templatePathsFound = new Dictionary<string, IVirtualFile>(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> templatePathsNotFound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public string GetTemplatePath(IVirtualDirectory fileDir)
        {
            try
            {
                if (templatePathsNotFound.Contains(fileDir.VirtualPath)) return null;

                var templateDir = fileDir;
                IVirtualFile templateFile;
                while (templateDir != null && templateDir.GetFile(defaultTemplateName) == null)
                {
                    var tmplPath = templateDir.VirtualPath;
                    if (tmplPath != null && templatePathsFound.TryGetValue(tmplPath, out templateFile))
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
            waiter.Reset();
            compilePages.Enqueue(pageToCompile);
        }

        private int runningThreads;
        public void CompileQueuedPages()
        {
            var compileInParallel = CompileInParallelWithNoOfThreads > 0;

            Log.Info($"Starting to compile {compilePages.Count}/{priorityCompilePages.Count} pages, " +
                     $"{(compileInParallel ? "In Parallel" : "Sequentially")}");

#if !NETSTANDARD1_6
            if (compileInParallel)
            {
                var threadsToRun = Math.Min(CompileInParallelWithNoOfThreads.GetValueOrDefault(), compilePages.Count);
                if (threadsToRun <= runningThreads) return;

                Log.Info($"Starting {threadsToRun} threads..");

                threadsToRun.Times(x => {
                    ThreadPool.QueueUserWorkItem(waitHandle => { try { CompileAllPages(); } catch { } });
                });
            }
            else
            {
                CompileAllPages();
            }
#else
            CompileAllPages();
#endif
        }

        private void CompileAllPages()
        {
            try
            {
                Interlocked.Increment(ref runningThreads);
                while (!compilePages.IsEmpty || !priorityCompilePages.IsEmpty)
                {
                    IViewPage viewPage;
                    if (compilePages.TryDequeue(out viewPage))
                    {
                        viewPage.Compile();
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref runningThreads);
                Log.Info($"Compilation threads remaining {runningThreads}...");
                waiter.Set();
            }
        }

        public void EnsureAllCompiled()
        {
            if (compilePages.IsEmpty && priorityCompilePages.IsEmpty) return;
            waiter.WaitOne(60 * 1000);
        }
    }

}

