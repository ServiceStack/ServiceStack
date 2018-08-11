using System;
using System.Collections.Concurrent;
using System.Linq;
using ServiceStack.IO;
using ServiceStack.VirtualPath;

namespace ServiceStack.Templates
{
    public interface ITemplatePages
    {
        TemplatePage ResolveLayoutPage(TemplatePage page, string layout);
        TemplatePage AddPage(string virtualPath, IVirtualFile file);
        TemplatePage GetPage(string virtualPath);
        TemplatePage TryGetPage(string path);
        TemplatePage OneTimePage(string contents, string ext);
        
        TemplatePage ResolveLayoutPage(TemplateCodePage page, string layout);
        TemplateCodePage GetCodePage(string virtualPath);

        DateTime GetLastModified(TemplatePage page);
    }

    public class TemplatePages : ITemplatePages
    {
        public TemplateContext Context { get; }

        public TemplatePages(TemplateContext context) => this.Context = context;

        public static string Layout = "layout";
        
        readonly ConcurrentDictionary<string, TemplatePage> pageMap = new ConcurrentDictionary<string, TemplatePage>(); 

        public virtual TemplatePage ResolveLayoutPage(TemplatePage page, string layout)
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

                if (pageMap.TryGetValue(layoutPath, out TemplatePage layoutPage))
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

        public virtual TemplatePage ResolveLayoutPage(TemplateCodePage page, string layout)
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

                if (pageMap.TryGetValue(layoutPath, out TemplatePage layoutPage))
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

        public TemplateCodePage GetCodePage(string virtualPath) => Context.GetCodePage(virtualPath)
            ?? (virtualPath?.Length > 0 && virtualPath[virtualPath.Length - 1] != '/' ? Context.GetCodePage(virtualPath + '/') : null);

        public virtual TemplatePage AddPage(string virtualPath, IVirtualFile file)
        {
            return pageMap[virtualPath] = new TemplatePage(Context, file);
        }

        public virtual TemplatePage TryGetPage(string path)
        {
            var santizePath = path.Replace('\\','/').TrimPrefixes("/").LastLeftPart('.');

            if (pageMap.TryGetValue(santizePath, out TemplatePage page)) 
                return page;

            return null;
        }

        public virtual TemplatePage GetPage(string pathInfo)
        {
            if (string.IsNullOrEmpty(pathInfo))
                return null;

            var santizePath = pathInfo.Replace('\\','/').TrimPrefixes("/");
            var isDirectory = santizePath.Length == 0 || santizePath[santizePath.Length - 1] == '/';

            TemplatePage page = null;
            var mappedPath = Context.GetPathMapping(nameof(TemplatePages), santizePath);
            if (mappedPath != null)
            {
                page = TryGetPage(mappedPath);
                if (page != null)
                    return page;
                Context.RemovePathMapping(nameof(TemplatePages), mappedPath);
            }

            var fileNameParts = santizePath.LastRightPart('/').SplitOnLast('.');
            var ext = fileNameParts.Length > 1 ? fileNameParts[1] : null;
            if (ext != null)
            {
                var registeredPageExt = Context.PageFormats.Any(x => x.Extension == ext);
                if (!registeredPageExt)
                    return null;
            }

            var filePath = santizePath.LastLeftPart('.');
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
                    Context.SetPathMapping(nameof(TemplatePages), santizePath, pageVirtualPath);
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
                        Context.SetPathMapping(nameof(TemplatePages), santizePath, pageVirtualPath);
                        return AddPage(pageVirtualPath, file);
                    }
                }
            }

            return null; 
        }

        private static readonly MemoryVirtualFiles TempFiles = new MemoryVirtualFiles();
        private static readonly InMemoryVirtualDirectory TempDir = new InMemoryVirtualDirectory(TempFiles, TemplateConstants.TempFilePath);

        public virtual TemplatePage OneTimePage(string contents, string ext)
        {
            var memFile = new InMemoryVirtualFile(TempFiles, TempDir)
            {
                FilePath = Guid.NewGuid().ToString("n") + "." + ext, 
                TextContents = contents,
            };
            
            var page = new TemplatePage(Context, memFile);
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
        
        public DateTime GetLastModified(TemplatePage page)
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

        public DateTime GetLastModifiedPage(TemplatePage page)
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
                        Context.GetPage(page.VirtualPath, partialPath, out TemplatePage partialPage, out _);
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
                        var file = Context.ProtectedFilters.ResolveFile(Context.VirtualFiles, page.VirtualPath, filePath);
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
                            Context.GetPage(page.VirtualPath, partialArg, out TemplatePage partialPage, out _);
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