using System;
using System.Linq;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using Microsoft.CSharp;
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
        public static ILog Log = LogManager.GetLogger(typeof(RazorViewManager));

        public Dictionary<string, RazorPage> Pages = new Dictionary<string, RazorPage>(StringComparer.OrdinalIgnoreCase);
        protected Dictionary<string, string> ViewNamesMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        protected IRazorConfig Config { get; set; }

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

            var files = this.PathProvider.GetAllMatchingFiles(pattern).Where(IsWatchedFile);

            // you can override IsWatchedFile to filter
            files.Each(x => AddPage(x));
        }


        private void ScanAssemblies()
        {
            if (this.Config.ScanAssemblies == null)
                return;

            foreach (var assembly in this.Config.ScanAssemblies)
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
            var newFile = GetVirutalFile(filePath);
            return AddPage(newFile);
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

            RazorPage page;
            if (this.Pages.TryGetValue(GetDictionaryPagePath(file), out page))
                return page;

            return TrackPage(file);
        }

        public virtual RazorPage AddPage(Type pageType)
        {
            var virtualPathAttr = pageType.FirstAttribute<VirtualPathAttribute>();
            if (virtualPathAttr == null || !this.IsWatchedFile(virtualPathAttr.VirtualPath))
                return null;

            var pagePath = virtualPathAttr.VirtualPath.TrimStart('~');
            RazorPage page;
            if (this.Pages.TryGetValue(GetDictionaryPagePath(pagePath), out page))
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
                PageHost = new RazorPageHost(PathProvider, file, transformer, new CSharpCodeProvider(), new Dictionary<string, string>()),
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
            var file = GetVirutalFile(pagePath);
            
            var page = new RazorPage
            {
                PageHost = file != null ? new RazorPageHost(PathProvider, file, transformer, new CSharpCodeProvider(), new Dictionary<string, string>()) : null,
                PageType = pageType,
                IsValid = true,
                File = file,
                VirtualPath = pagePath,
            };

            AddPage(page, pagePath);
            return page;
        }

        protected virtual RazorPage AddPage(RazorPage page, string pagePath = null)
        {
            pagePath = pagePath != null
                ? GetDictionaryPagePath(pagePath)
                : GetDictionaryPagePath(page.PageHost.File);

            this.Pages[pagePath] = page;

            //Views should be uniquely named and stored in any deep folder structure
            if (pagePath.StartsWithIgnoreCase("/views/"))
            {
                var viewName = pagePath.SplitOnLast('.').First().SplitOnLast('/').Last();
                ViewNamesMap[viewName] = pagePath;
            }

            return page;
        }

        public virtual RazorPage GetPage(string absolutePath)
        {
            RazorPage page;
            this.Pages.TryGetValue(absolutePath, out page);
            return page;
        }

        public virtual RazorPage GetPageByPathInfo(string pathInfo)
        {
            RazorPage page;
            if (this.Pages.TryGetValue(pathInfo, out page))
                return page;

            if (this.Pages.TryGetValue(Path.ChangeExtension(pathInfo, Config.RazorFileExtension), out page))
                return page;

            if (this.Pages.TryGetValue(CombinePaths(pathInfo, Config.DefaultPageName), out page))
                return page;

            return null;
        }

        public virtual RazorPage GetPage(IRequest request, object dto)
        {
            var normalizePath = NormalizePath(request, dto);
            return GetPage(normalizePath);
        }

        public virtual RazorPage GetPageByName(string pageName)
        {
            return GetPageByName(pageName, null, null);
        }

        private static string CombinePaths(params string[] paths)
        {
            var combinedPath = PathUtils.CombinePaths(paths);
            if (!combinedPath.StartsWith("/"))
                combinedPath = "/" + combinedPath;
            return combinedPath;
        }

        public virtual RazorPage GetPageByName(string pageName, IRequest request, object dto)
        {
            RazorPage page = null;
            var htmlPageName = Path.ChangeExtension(pageName, Config.RazorFileExtension);

            if (request != null)
            {
                var contextRelativePath = NormalizePath(request, dto) ?? "/views/";

                string contextParentDir = contextRelativePath;
                do
                {
                    contextParentDir = (contextParentDir ?? "").SplitOnLast('/').First();

                    var relativePath = CombinePaths(contextParentDir, htmlPageName);
                    if (this.Pages.TryGetValue(relativePath, out page))
                        return page;

                } while (!string.IsNullOrEmpty(contextParentDir));
            }

            //var sharedPath = "/view/shared/{0}".Fmt(htmlPageName);
            //if (this.Pages.TryGetValue(sharedPath, out page))
            //    return page;

            string viewPath;
            if (ViewNamesMap.TryGetValue(pageName, out viewPath))
                this.Pages.TryGetValue(viewPath, out page);

            return page ?? GetPageByPathInfo("/" + htmlPageName);
        }

        static char[] InvalidFileChars = new[] { '<', '>', '`' }; //Anonymous or Generic type names
        private string NormalizePath(IRequest request, object dto)
        {
            if (dto != null && !(dto is DynamicRequestObject)) // this is for a view inside /views
            {
                //if we have a view name, use it.
                var viewName = request.GetView();

                if (string.IsNullOrWhiteSpace(viewName))
                {
                    //use the response DTO name
                    viewName = dto.GetType().Name;
                }
                if (string.IsNullOrWhiteSpace(viewName))
                {
                    //the request use the request DTO name.
                    viewName = request.OperationName;
                }

                var isInvalidName = viewName.IndexOfAny(InvalidFileChars) >= 0;
                if (!isInvalidName)
                {
                    return CombinePaths("views", Path.ChangeExtension(viewName, Config.RazorFileExtension));
                }
            }

            // path/to/dir/default.cshtml
            var path = request.PathInfo;
            var defaultIndex = CombinePaths(path, Config.DefaultPageName);
            if (Pages.ContainsKey(defaultIndex))
                return defaultIndex;

            return Path.ChangeExtension(path, Config.RazorFileExtension);
        }

        public virtual bool IsWatchedFile(IVirtualFile file)
        {
            return this.Config.RazorFileExtension.EndsWithIgnoreCase(file.Extension);
        }

        public virtual bool IsWatchedFile(string fileName)
        {
            return this.Config.RazorFileExtension.EndsWithIgnoreCase(Path.GetExtension(fileName));
        }

        public virtual string GetDictionaryPagePath(string relativePath)
        {
            if (relativePath.ToLowerInvariant().StartsWith("/views/"))
            {
                //re-write the /views path
                //so we can uniquely get views by
                //ResponseDTO/RequestDTO type.
                //PageResolver:NormalizePath()
                //knows how to resolve DTO views.
                return "/views/" + Path.GetFileName(relativePath);
            }
            return relativePath;
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
            });

            if (startupPrecompilationTasks != null)
                startupPrecompilationTasks.Add(task);

            return task;
        }

        public virtual void EnsureCompiled(RazorPage page)
        {
            if (page == null) return;
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

        public virtual IVirtualFile GetVirutalFile(string ospath)
        {
            var relative = GetRelativePath(ospath);
            return this.PathProvider.GetFile(relative);
        }
        #endregion
    }
}