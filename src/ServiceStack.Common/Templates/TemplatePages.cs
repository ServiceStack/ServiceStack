using System;
using System.Collections.Concurrent;
using ServiceStack.IO;

namespace ServiceStack.Templates
{
    public interface ITemplatePages
    {
        TemplatePage ResolveLayoutPage(TemplatePage page, string layout);
        TemplatePage AddPage(string virtualPath, IVirtualFile file);
        TemplatePage GetPage(string virtualPath);
        TemplatePage GetOrCreatePage(string virtualPath);
    }

    public class TemplatePages : ITemplatePages
    {
        public TemplatePagesContext Context { get; }

        public TemplatePages(TemplatePagesContext context) => this.Context = context;

        public static string Layout = "layout";
        
        readonly ConcurrentDictionary<string, TemplatePage> pageMap = new ConcurrentDictionary<string, TemplatePage>(); 

        public virtual TemplatePage ResolveLayoutPage(TemplatePage page, string layout)
        {
            if (!page.HasInit)
                throw new ArgumentException($"Page {page.File.VirtualPath} has not been initialized");

            var layoutWithoutExt = (layout ?? Context.DefaultLayoutPage).LeftPart('.');

            var dir = page.File.Directory;
            do
            {
                var layoutPath = dir.VirtualPath.CombineWith(layoutWithoutExt);

                if (pageMap.TryGetValue(layoutPath, out TemplatePage layoutPage))
                    return layoutPage;

                foreach (var format in Context.PageFormats)
                {
                    var layoutFile = dir.GetFile($"{layoutWithoutExt}.{format.Extension}");
                    if (layoutFile != null)
                        return AddPage(layoutPath, layoutFile);
                }
                
                dir = dir.ParentDirectory;

            } while (!dir.IsRoot);
            
            return null;
        }

        public virtual TemplatePage AddPage(string virtualPath, IVirtualFile file)
        {
            return pageMap[virtualPath] = new TemplatePage(Context, file);
        }

        public virtual TemplatePage GetPage(string path)
        {
            var santizePath = path.Replace('\\','/').TrimPrefixes("/").LastLeftPart('.');

            return pageMap.TryGetValue(santizePath, out TemplatePage page) 
                ? page 
                : null;
        }

        public virtual TemplatePage GetOrCreatePage(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            
            var santizePath = path.Replace('\\','/').TrimPrefixes("/").LastLeftPart('.');

            var page = GetPage(santizePath);
            if (page != null)
                return page;

            foreach (var format in Context.PageFormats)
            {
                var file = !santizePath.EndsWith("/")
                    ? Context.VirtualFiles.GetFile($"{santizePath}.{format.Extension}")
                    : Context.VirtualFiles.GetFile($"{santizePath}{Context.IndexPage}.{format.Extension}");

                if (file != null)
                    return AddPage(file.VirtualPath.WithoutExtension(), file);
            }

            return null; 
        }
    }
}