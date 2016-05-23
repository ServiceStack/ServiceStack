using System;
using System.Linq;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.CSharp;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Razor.Compilation;
using ServiceStack.Razor.Compilation.CodeTransformers;
using ServiceStack.Razor.Managers.RazorGen;
using ServiceStack.Web;

namespace ServiceStack.Razor.Managers
{
    /// <summary>
    /// This view manager is responsible for keeping track of all the 
    /// available Razor views and states of Razor pages.
    /// </summary>
    public class RazorViewManager
    {
        const string DefaultLayoutFile = RazorPageResolver.DefaultLayoutName + ".cshtml";

        public static ILog Log = LogManager.GetLogger(typeof(RazorViewManager));

        public Dictionary<string, RazorPage> Pages = new Dictionary<string, RazorPage>(StringComparer.OrdinalIgnoreCase);
        protected Dictionary<string, string> ViewNamesMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        protected IRazorConfig Config { get; set; }

        public bool IncludeDebugInformation { get; set; }
        public Action<CompilerParameters> CompileFilter { get; set; }
        public bool CheckLastModifiedForChanges { get; set; }

        protected IVirtualPathProvider PathProvider = null;

        public RazorViewManager(IRazorConfig viewConfig, IVirtualPathProvider virtualPathProvider)
        {
            this.Config = viewConfig;
            this.PathProvider = virtualPathProvider;
        }

        public void Init()
        {
            if (Config.WaitForPrecompilationOnStartup.GetValueOrDefault())
                startupPrecompilationTasks = new List<Task>();

            ScanForRazorPages();

            if (Config.WaitForPrecompilationOnStartup.GetValueOrDefault())
            {
                Task.WaitAll(startupPrecompilationTasks.ToArray());
                startupPrecompilationTasks = null;
            }
        }

        private void ScanForRazorPages()
        {
            ScanAssemblies();
            ScanPaths();
        }

        private void ScanPaths()
        {
            var pattern = Path.ChangeExtension("*", this.Config.RazorFileExtension);

            var files = PathProvider.GetAllMatchingFiles(pattern);
            files.Each(x => AddPage(x));
        }

        private void ScanAssemblies()
        {
            foreach (var assembly in this.Config.LoadFromAssemblies)
            {
                foreach (var type in assembly.GetTypes()
                    .Where(w => w.FirstAttribute<GeneratedCodeAttribute>() != null
                        && w.FirstAttribute<GeneratedCodeAttribute>().Tool == "RazorGenerator"
                        && w.FirstAttribute<VirtualPathAttribute>() != null))
                {
                    AddPage(type);
                }
            }
        }

        public virtual RazorPage AddPage(string filePath)
        {
            var newFile = GetVirtualFile(filePath);
            return AddPage(newFile);
        }

        public virtual RazorPage RefreshPage(string filePath)
        {
            var file = GetVirtualFile(filePath);
            var page = GetPage(file);
            if (page == null)
                throw new ArgumentException("No RazorPage found at: " + filePath);

            InvalidatePage(page, compile: true);
            return page;
        }

        public virtual void InvalidatePage(RazorPage page, bool compile = true)
        {
            if (page.IsValid || page.IsCompiling)
            {
                lock (page.SyncRoot)
                {
                    page.IsValid = false;
                }
            }

            if (compile)
                PrecompilePage(page);
        }

        public virtual RazorPage AddPage(IVirtualFile file)
        {
            if (!IsWatchedFile(file))
                return null;

            var page = GetPage(file);
            if (page != null)
                return page;

            return TrackPage(file);
        }

        public virtual RazorPage AddPage(Type pageType)
        {
            var virtualPathAttr = pageType.FirstAttribute<VirtualPathAttribute>();
            if (virtualPathAttr == null || !this.IsWatchedFile(virtualPathAttr.VirtualPath))
                return null;

            var pagePath = virtualPathAttr.VirtualPath.TrimStart('~');
            var page = GetPage(pagePath);
            if (page != null)
                return page;

            return TrackPage(pageType);
        }

        public virtual RazorPage TrackPage(IVirtualFile file)
        {
            //get the base type.
            var pageBaseType = this.Config.PageBaseType;

            var transformer = new RazorViewPageTransformer(pageBaseType);

            //create a RazorPage
            var page = new RazorPage
            {
                PageHost = CreatePageHost(file, transformer),
                IsValid = false,
                File = file,
                VirtualPath = file.VirtualPath,
            };

            //add it to our pages dictionary.
            AddPage(page);

            if (Config.PrecompilePages.GetValueOrDefault())
                PrecompilePage(page);

            return page;
        }

        public virtual RazorPage TrackPage(Type pageType)
        {
            var pageBaseType = this.Config.PageBaseType;
            var transformer = new RazorViewPageTransformer(pageBaseType);

            var pagePath = pageType.FirstAttribute<VirtualPathAttribute>().VirtualPath.TrimStart('~');
            var file = GetVirtualFile(pagePath);
            
            var page = new RazorPage
            {
                PageHost = file != null ? CreatePageHost(file, transformer) : null,
                PageType = pageType,
                IsValid = true,
                File = file,
                VirtualPath = pagePath,
            };

            AddPage(page, pagePath);
            return page;
        }

        private RazorPageHost CreatePageHost(IVirtualFile file, RazorViewPageTransformer transformer)
        {
            return new RazorPageHost(PathProvider, file, transformer, new CSharpCodeProvider(), new Dictionary<string, string>())
            {
                IncludeDebugInformation = IncludeDebugInformation,
                CompileFilter = CompileFilter,
            };
        }

        protected virtual RazorPage AddPage(RazorPage page, string pagePath = null)
        {
            pagePath = pagePath != null
                ? GetDictionaryPagePath(pagePath)
                : GetDictionaryPagePath(page.PageHost.File);

            Pages[pagePath] = page;

            //Views should be uniquely named and stored in any deep folder structure
            if (pagePath.StartsWithIgnoreCase("views/") && !pagePath.EndsWithIgnoreCase(DefaultLayoutFile))
            {
                var viewName = pagePath.LastLeftPart('.').LastRightPart('/');
                ViewNamesMap[viewName] = pagePath;
            }

            return page;
        }

        public virtual RazorPage GetPage(string absolutePath)
        {
            RazorPage page;
            Pages.TryGetValue(GetDictionaryPagePath(absolutePath), out page);
            return page;
        }

        public RazorPage GetPage(IVirtualFile file)
        {
            return GetPage(file.VirtualPath);
        }

        private static string CombinePaths(params string[] paths)
        {
            var combinedPath = PathUtils.CombinePaths(paths);
            if (combinedPath.StartsWith("/"))
                combinedPath = combinedPath.Substring(1);
            return combinedPath;
        }

        public virtual RazorPage GetViewPage(string pageName)
        {
            string viewPath;
            if (ViewNamesMap.TryGetValue(pageName.ToLowerInvariant(), out viewPath))
                return GetPage(viewPath);
            return null;
        }

        public virtual RazorPage GetContentPage(string pathInfo)
        {
            return GetPage(Path.ChangeExtension(pathInfo, Config.RazorFileExtension))
                   ?? GetPage(CombinePaths(pathInfo, Config.DefaultPageName));
        }

        public virtual RazorPage GetLayoutPage(string layoutName, RazorPage page, IRequest request, object dto)
        {
            var layoutFile = Path.ChangeExtension(layoutName, Config.RazorFileExtension);
            // layoutName may or may not contain the .cshtml extension, the below forces it not to.
            layoutName = Path.GetFileNameWithoutExtension(layoutFile);

            var contextRelativePath = page.VirtualPath;
            string contextParentDir = contextRelativePath;
            do
            {
                contextParentDir = (contextParentDir ?? "").LastLeftPart('/');

                var path = CombinePaths(contextParentDir, layoutFile);
                var layoutPage = GetPage(path);
                if (layoutPage != null)
                    return layoutPage;

            } while (!string.IsNullOrEmpty(contextParentDir) && contextParentDir.Contains('/'));

            if (layoutName != RazorPageResolver.DefaultLayoutName)
                return GetViewPage(layoutName);

            return GetPage(CombinePaths("/views/shared/", layoutFile))
                   ?? GetPage(CombinePaths("/views/", layoutFile)); //backwards compatibility fallback
        }

        public virtual RazorPage GetPartialPage(IHttpRequest httpReq, string partialName)
        {
            var normalizedPathInfo = httpReq != null
                ? httpReq.PathInfo
                : "/";

            if (httpReq != null)
            {
                if (!httpReq.RawUrl.EndsWith("/"))
                    normalizedPathInfo = normalizedPathInfo.ParentDirectory();

                normalizedPathInfo = normalizedPathInfo.CombineWith(partialName).TrimStart('/');
            }

            // Look for partial from same directory or view page
            return GetContentPage(normalizedPathInfo)
                ?? GetContentPage(normalizedPathInfo.CombineWith(RazorFormat.Instance.DefaultPageName))
                ?? GetViewPage(partialName);
        }

        public virtual bool IsWatchedFile(IVirtualFile file)
        {
            return file != null && this.Config.RazorFileExtension.EndsWithIgnoreCase(file.Extension);
        }

        public virtual bool IsWatchedFile(string fileName)
        {
            return this.Config.RazorFileExtension.EndsWithIgnoreCase(Path.GetExtension(fileName));
        }

        public virtual string GetDictionaryPagePath(string relativePath)
        {
            var path = relativePath.ToLower();
            return path[0] == '/'
                ? path.Substring(1)
                : path;
        }

        public virtual string GetDictionaryPagePath(IVirtualFile file)
        {
            return GetDictionaryPagePath(file.VirtualPath);
        }

        private List<Task> startupPrecompilationTasks;

        protected virtual Task<RazorPage> PrecompilePage(RazorPage page)
        {
            page.MarkedForCompilation = true;

            var task = Task.Factory.StartNew(() =>
            {
                try
                {
                    EnsureCompiled(page);

                    if (page.CompileException != null)
                        Log.ErrorFormat("Precompilation of Razor page '{0}' failed: {1}", page.File.Name, page.CompileException.Message);
                }
                catch (Exception ex)
                {
                    Log.ErrorFormat("Precompilation of Razor page '{0}' failed: {1}", page.File.Name, ex.Message);
                }
                return page;
            }, default(CancellationToken), TaskCreationOptions.None, TaskScheduler.Default);

            if (startupPrecompilationTasks != null)
                startupPrecompilationTasks.Add(task);

            return task;
        }

        public virtual void EnsureCompiled(RazorPage page)
        {
            if (page == null) return;
            if (CheckLastModifiedForChanges && page.IsValid)
            {
                lock (page.SyncRoot)
                {
                    var prevLastModified = page.File.LastModified;
                    page.File.Refresh();
                    page.IsValid = prevLastModified == page.File.LastModified;
                }
            }

            if (page.IsValid) return;
            if (page.PageHost == null)
            {
                Log.WarnFormat("Could not find virtualPath for compiled Razor page '{0}'.", page.VirtualPath);
                return;
            }

            lock (page.SyncRoot)
            {
                if (page.IsValid) return;

                var compileTimer = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    page.IsCompiling = true;
                    page.CompileException = null;

                    var type = page.PageHost.Compile();

                    page.PageType = type;

                    page.IsValid = true;

                    compileTimer.Stop();

                    if (Log.IsDebugEnabled)
                        Log.DebugFormat("Compiled Razor page '{0}' in {1}ms.", page.File.Name, compileTimer.ElapsedMilliseconds);
                }
                catch (HttpCompileException ex)
                {
                    page.CompileException = ex;
                }
                finally
                {
                    page.IsCompiling = false;
                    page.MarkedForCompilation = false;
                }
            }
        }

        #region FileSystemWatcher Handlers

        public virtual string GetRelativePath(string ospath)
        {
            if (Config.ScanRootPath == null)
                return ospath;

            var relative = ospath
                .Replace(Config.ScanRootPath, "")
                .Replace(this.PathProvider.RealPathSeparator, "/");
            return relative;
        }

        public virtual IVirtualFile GetVirtualFile(string ospath)
        {
            var relative = GetRelativePath(ospath);
            return this.PathProvider.GetFile(relative);
        }
        #endregion
    }
}