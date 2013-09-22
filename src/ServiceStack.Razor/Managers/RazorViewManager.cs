using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Microsoft.CSharp;
using ServiceStack.Common;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Razor.Compilation;
using ServiceStack.Razor.Managers.RazorGen;
using ServiceStack.Server;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.Utils;

namespace ServiceStack.Razor.Managers
{
    /// <summary>
    /// This view manager is responsible for keeping track of all the 
    /// available Razor views and states of Razor pages.
    /// </summary>
    public class RazorViewManager
    {
        public static ILog Log = LogManager.GetLogger(typeof(RazorViewManager));

        public Dictionary<string, RazorPage> Pages = new Dictionary<string, RazorPage>(StringComparer.InvariantCultureIgnoreCase);
        protected Dictionary<string, string> ViewNamesMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        protected IRazorConfig Config { get; set; }

        protected IVirtualPathProvider PathProvider = null;

        public RazorViewManager(IRazorConfig viewConfig, IVirtualPathProvider virtualPathProvider)
        {
            this.Config = viewConfig;
            this.PathProvider = virtualPathProvider;
        }

        public void Init()
        {
            ScanForRazorPages();
        }

        private void ScanForRazorPages()
        {
            var pattern = Path.ChangeExtension("*", this.Config.RazorFileExtension);

            var files = this.PathProvider.GetAllMatchingFiles(pattern)
                            .Where(IsWatchedFile);

            // you can override IsWatchedFile to filter
            files.Each(x => TrackPage(x));
        }

        public virtual RazorPage AddPage(string filePath)
        {
            var newFile = GetVirutalFile(filePath);
            return AddPage(newFile);
        }

        public virtual RazorPage AddPage(IVirtualFile file)
        {
            return IsWatchedFile(file) 
                ? TrackPage(file)
                : null;
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
                File = file
            };

            //add it to our pages dictionary.
            AddPage(page);
            
            return page;
        }

        protected virtual RazorPage AddPage(RazorPage page)
        {
            var pagePath = GetDictionaryPagePath(page.PageHost.File);

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

        public virtual RazorPage GetPage(IHttpRequest request, object dto)
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

        public virtual RazorPage GetPageByName(string pageName, IHttpRequest request, object dto)
        {
            RazorPage page = null;
            var htmlPageName = Path.ChangeExtension(pageName, Config.RazorFileExtension);

            if (request != null)
            {
                var contextRelativePath = NormalizePath(request, dto);

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

            return page;
        }

        static char[] InvalidFileChars = new[]{'<','>','`'}; //Anonymous or Generic type names
        private string NormalizePath(IHttpRequest request, object dto)
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