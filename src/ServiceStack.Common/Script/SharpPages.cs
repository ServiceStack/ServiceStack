using System;
using System.Collections.Concurrent;
using System.Linq;
using ServiceStack.IO;

namespace ServiceStack.Script
{
    public interface ISharpPages
    {
        SharpPage ResolveLayoutPage(SharpPage page, string layout);
        SharpPage AddPage(string virtualPath, IVirtualFile file);
        SharpPage GetPage(string virtualPath);
        SharpPage TryGetPage(string path);
        SharpPage OneTimePage(string contents, string ext);
        
        SharpPage ResolveLayoutPage(SharpCodePage page, string layout);
        SharpCodePage GetCodePage(string virtualPath);

        DateTime GetLastModified(SharpPage page);
    }

    public partial class SharpPages : ISharpPages
    {
        public ScriptContext Context { get; }

        public SharpPages(ScriptContext context) => this.Context = context;

        public static string Layout = "layout";
        
        readonly ConcurrentDictionary<string, SharpPage> pageMap = new ConcurrentDictionary<string, SharpPage>(); 

        public virtual SharpPage ResolveLayoutPage(SharpPage page, string layout)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));
            
            if (!page.HasInit)
                throw new ArgumentException($"Page {page.File.VirtualPath} has not been initialized");

            if (page.IsLayout)
                return null;
            
            var layoutWithoutExt = (layout ?? Context.DefaultLayoutPage).LeftPart('.');

            var dir = page.File.Directory;
            do
            {
                var layoutPath = (dir.VirtualPath ?? "").CombineWith(layoutWithoutExt);

                if (pageMap.TryGetValue(layoutPath, out SharpPage layoutPage))
                    return layoutPage;

                foreach (var format in Context.PageFormats)
                {
                    var layoutFile = dir.GetFile($"{layoutWithoutExt}.{format.Extension}");
                    if (layoutFile != null)
                        return AddPage(layoutPath, layoutFile);
                }
                
                if (dir.IsRoot)
                    break;
                
                dir = dir.ParentDirectory;

            } while (dir != null);
            
            return null;
        }

        public virtual SharpPage ResolveLayoutPage(SharpCodePage page, string layout)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));
            
            if (!page.HasInit)
                throw new ArgumentException($"Page {page.VirtualPath} has not been initialized");

            var layoutWithoutExt = (layout ?? Context.DefaultLayoutPage).LeftPart('.');

            var lastDirPos = page.VirtualPath.LastIndexOf('/');
            var dirPath = lastDirPos >= 0
                ? page.VirtualPath.Substring(0, lastDirPos)
                : null;
            var dir = !string.IsNullOrEmpty(dirPath) 
                ? Context.VirtualFiles.GetDirectory(dirPath) 
                : Context.VirtualFiles.RootDirectory;
            do
            {
                var layoutPath = (dir.VirtualPath ?? "").CombineWith(layoutWithoutExt);

                if (pageMap.TryGetValue(layoutPath, out SharpPage layoutPage))
                    return layoutPage;

                foreach (var format in Context.PageFormats)
                {
                    var layoutFile = dir.GetFile($"{layoutWithoutExt}.{format.Extension}");
                    if (layoutFile != null)
                        return AddPage(layoutPath, layoutFile);
                }
                
                if (dir.IsRoot)
                    break;
                
                dir = dir.ParentDirectory;

            } while (dir != null);
            
            return null;
        }

        public SharpCodePage GetCodePage(string virtualPath) => Context.GetCodePage(virtualPath)
            ?? (virtualPath?.Length > 0 && virtualPath[virtualPath.Length - 1] != '/' ? Context.GetCodePage(virtualPath + '/') : null);

        public virtual SharpPage AddPage(string virtualPath, IVirtualFile file)
        {
            return pageMap[virtualPath] = new SharpPage(Context, file);
        }

        public virtual SharpPage TryGetPage(string path)
        {
            var santizePath = path.Replace('\\','/').TrimPrefixes("/").LastLeftPart('.');

            if (pageMap.TryGetValue(santizePath, out SharpPage page)) 
                return page;

            return null;
        }

        public virtual SharpPage GetPage(string pathInfo)
        {
            if (string.IsNullOrEmpty(pathInfo))
                return null;

            var sanitizePath = pathInfo.Replace('\\','/').TrimPrefixes("/");
            var isDirectory = sanitizePath.Length == 0 || sanitizePath[sanitizePath.Length - 1] == '/';

            SharpPage page = null;
            var mappedPath = Context.GetPathMapping(nameof(SharpPages), sanitizePath);
            if (mappedPath != null)
            {
                page = TryGetPage(mappedPath);
                if (page != null)
                    return page;
                Context.RemovePathMapping(nameof(SharpPages), mappedPath);
            }

            var fileNameParts = sanitizePath.LastRightPart('/').SplitOnLast('.');
            var ext = fileNameParts.Length > 1 ? fileNameParts[1] : null;
            if (ext != null)
            {
                var registeredPageExt = Context.PageFormats.Any(x => x.Extension == ext);
                if (!registeredPageExt)
                    return null;
            }

            var filePath = sanitizePath.LastLeftPart('.');
            page = TryGetPage(filePath) ?? (!isDirectory ? TryGetPage(filePath + '/') : null);
            if (page != null)
                return page;

            foreach (var format in Context.PageFormats)
            {
                var file = !isDirectory
                    ? Context.VirtualFiles.GetFile(filePath + "." + format.Extension)
                    : Context.VirtualFiles.GetFile(filePath + Context.IndexPage + "." + format.Extension);

                if (file != null)
                {
                    var pageVirtualPath = file.VirtualPath.WithoutExtension();
                    Context.SetPathMapping(nameof(SharpPages), sanitizePath, pageVirtualPath);
                    return AddPage(pageVirtualPath, file);
                }
            }

            if (!isDirectory)
            {
                var tryFilePath = filePath + '/';
                foreach (var format in Context.PageFormats)
                {
                    var file = Context.VirtualFiles.GetFile(tryFilePath + Context.IndexPage + "." + format.Extension);
                    if (file != null)
                    {
                        var pageVirtualPath = file.VirtualPath.WithoutExtension();
                        Context.SetPathMapping(nameof(SharpPages), sanitizePath, pageVirtualPath);
                        return AddPage(pageVirtualPath, file);
                    }
                }
            }

            return null; 
        }

        private static readonly MemoryVirtualFiles TempFiles = new MemoryVirtualFiles();
        private static readonly InMemoryVirtualDirectory TempDir = new InMemoryVirtualDirectory(TempFiles, ScriptConstants.TempFilePath);

        public virtual SharpPage OneTimePage(string contents, string ext)
        {
            var memFile = new InMemoryVirtualFile(TempFiles, TempDir)
            {
                FilePath = Guid.NewGuid().ToString("n") + "." + ext, 
                TextContents = contents,
            };
            
            var page = new SharpPage(Context, memFile);
            try
            {
                page.Init().Wait(); // Safe as Memory Files are non-blocking
                return page;
            }
            catch (AggregateException e)
            {
                throw e.UnwrapIfSingleException();
            }
        }
        
        public DateTime GetLastModified(SharpPage page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            page.File.Refresh();
            var maxLastModified = page.File.LastModified;

            var layout = page.IsLayout ? null : page.LayoutPage ?? ResolveLayoutPage(page, null);
            if (layout != null)
            {
                var layoutLastModified = GetLastModifiedPage(layout);
                if (layoutLastModified > maxLastModified)
                    maxLastModified = layoutLastModified;
            }

            var pageLastModified = GetLastModifiedPage(page);
            if (pageLastModified > maxLastModified)
                maxLastModified = pageLastModified;

            return maxLastModified;
        }

        public DateTime GetLastModifiedPage(SharpPage page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            page.File.Refresh();
            var maxLastModified = page.File.LastModified;

            var varFragments = page.PageFragments.OfType<PageVariableFragment>();
            foreach (var fragment in varFragments)
            {
                var filter = fragment.FilterExpressions?.FirstOrDefault();
                if (filter?.Name == "partial")
                {
                    if (fragment.InitialValue is string partialPath)
                    {
                        Context.TryGetPage(page.VirtualPath, partialPath, out SharpPage partialPage, out _);
                        if (partialPage == null && partialPath[0] != '_')
                            Context.TryGetPage(page.VirtualPath, $"_{partialPath}-partial", out partialPage, out _);
                        
                        maxLastModified = GetMaxLastModified(partialPage?.File, maxLastModified);

                        if (partialPage?.HasInit == true)
                        {
                            var partialLastModified = GetLastModifiedPage(partialPage);
                            if (partialLastModified > maxLastModified)
                                maxLastModified = partialLastModified;
                        }
                    }
                }
                else if (filter?.Name != null && Context.FileFilterNames.Contains(filter?.Name))
                {
                    if (fragment.InitialValue is string filePath)
                    {
                        var file = Context.ProtectedMethods.ResolveFile(Context.VirtualFiles, page.VirtualPath, filePath);
                        maxLastModified = GetMaxLastModified(file, maxLastModified);
                    }
                }
                
                var lastFilter = fragment.FilterExpressions?.LastOrDefault();
                if (lastFilter?.Name == "selectPartial")
                {
                    if (lastFilter.Arguments.FirstOrDefault() is JsLiteral argLiteral && argLiteral.Value is string partialArg)
                    {
                        if (!string.IsNullOrEmpty(partialArg))
                        {
                            Context.TryGetPage(page.VirtualPath, partialArg, out SharpPage partialPage, out _);
                            if (partialPage == null && partialArg[0] != '_')
                                Context.TryGetPage(page.VirtualPath, $"_{partialArg}-partial", out partialPage, out _);

                            maxLastModified = GetMaxLastModified(partialPage?.File, maxLastModified);

                            if (partialPage?.HasInit == true)
                            {
                                var partialLastModified = GetLastModifiedPage(partialPage);
                                if (partialLastModified > maxLastModified)
                                    maxLastModified = partialLastModified;
                            }
                        }
                    }
                }
            }

            return maxLastModified;
        }

        private DateTime GetMaxLastModified(IVirtualFile file, DateTime maxLastModified)
        {
            if (file == null)
                return maxLastModified;

            file.Refresh();
            return file.LastModified > maxLastModified
                ? file.LastModified
                : maxLastModified;
        }
    }
}