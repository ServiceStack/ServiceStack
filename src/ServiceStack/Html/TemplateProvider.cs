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
        private static readonly ILog Log = LogManager.GetLogger(typeof(TemplateProvider));

        string defaultTemplateName;

        public TemplateProvider(string defaultTemplateName)
        {
            this.defaultTemplateName = defaultTemplateName;
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
        public void StartCompiling()
        {
            Log.InfoFormat("Starting to compile {0}/{1} pages",
                compilePages.Count, priorityCompilePages.Count);

            var threadsToRun = Math.Min(Environment.ProcessorCount * 2, compilePages.Count);
            if (threadsToRun <= runningThreads) return;

            Log.InfoFormat("Starting {0} threads..", threadsToRun);

            threadsToRun.Times(x => {
                ThreadPool.QueueUserWorkItem(waitHandle => {
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
                });
            });
        }
    }

}

