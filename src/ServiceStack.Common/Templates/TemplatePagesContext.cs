using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ServiceStack.Configuration;
using ServiceStack.IO;
using ServiceStack.Text;
using ServiceStack.VirtualPath;
#if NETSTANDARD1_3
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public class TemplatePagesContext
    {
        public List<PageFormat> PageFormats { get; set; } = new List<PageFormat>();
        
        public string IndexPage { get; set; } = "index";

        public string DefaultLayoutPage { get; set; } = "_layout";
        
        public string LayoutVarName { get; set; } = "layout";

        public bool CheckModifiedPages { get; set; } = false;

        public ITemplatePages Pages { get; set; }

        public IVirtualPathProvider VirtualFiles { get; set; } = new MemoryVirtualFiles();
        
        public bool DebugMode { get; set; }

        public PageFormat GetFormat(string extension) => PageFormats.FirstOrDefault(x => x.Extension == extension);

        public List<Type> ScanTypes { get; set; } = new List<Type>();

        public List<Assembly> ScanAssemblies{ get; set; } = new List<Assembly>();

        public IContainer Container { get; set; } = new SimpleContainer();
        
        public List<TemplateFilter> TemplateFilters { get; } = new List<TemplateFilter>();

        public List<TemplateCode> CodePages { get; } = new List<TemplateCode>();

        public TemplatePagesContext()
        {
            Pages = new TemplatePages(this);
            PageFormats.Add(new HtmlPageFormat());
        }

        public TemplatePagesContext Init()
        {
            Container.AddSingleton(() => Pages);

            foreach (var type in ScanTypes)
            {
                ScanType(type);
            }

            foreach (var assembly in ScanAssemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    ScanType(type);
                }
            }

            foreach (var filter in TemplateFilters)
            {
                if (filter.Pages == null)
                    filter.Pages = Pages;

                filter.Init();
            }

            foreach (var page in CodePages)
            {
                if (page.Pages == null)
                    page.Pages = Pages;

                page.Init();
            }

            return this;
        }

        public TemplatePagesContext ScanType(Type type)
        {
            if (typeof(TemplateFilter).IsAssignableFromType(type))
            {
                Container.AddSingleton(type);
                var filter = (TemplateFilter)Container.Resolve(type);
                TemplateFilters.Add(filter);
            }
            else if (typeof(TemplateCode).IsAssignableFromType(type))
            {
                Container.AddSingleton(type);
                var codePage = (TemplateCode)Container.Resolve(type);
                CodePages.Add(codePage);
            }
            return this;
        }
    }

    public class PageFormat
    {
        public string ArgsPrefix { get; set; } = "---";

        public string ArgsSuffix { get; set; } = "---";
        
        public string Extension { get; set; }

        public string ContentType { get; set; } = MimeTypes.PlainText;

        public Func<object, string> EncodeValue { get; set; }

        public Func<TemplatePage, TemplatePage> ResolveLayout { get; set; }

        public PageFormat()
        {
            EncodeValue = DefaultEncodeValue;
            ResolveLayout = DefaultResolveLayout;
        }

        public string DefaultEncodeValue(object value)
        {
            if (value is IRawString rawString)
                return rawString.ToRawString();
            
            var str = value.ToString();
            if (str == string.Empty)
                return string.Empty;

            return str;
        }

        public TemplatePage DefaultResolveLayout(TemplatePage page)
        {
            page.Args.TryGetValue(TemplatePages.Layout, out string layout);
            return page.Context.Pages.ResolveLayoutPage(page, layout);
        }
    }

    public class HtmlPageFormat : PageFormat
    {
        public HtmlPageFormat()
        {
            ArgsPrefix = "<!--";
            ArgsSuffix = "-->";
            Extension = "html";
            ContentType = MimeTypes.Html;
            EncodeValue = HtmlEncodeValue;
            ResolveLayout = HtmlResolveLayout;
        }
        
        public static string HtmlEncodeValue(object value)
        {
            if (value == null)
                return string.Empty;
            
            if (value is IHtmlString htmlString)
                return htmlString.ToHtmlString();

            if (value is IRawString rawString)
                return rawString.ToRawString();
            
            var str = value.ToString();
            if (str == string.Empty)
                return string.Empty;

            return StringUtils.HtmlEncode(str);
        }

        public TemplatePage HtmlResolveLayout(TemplatePage page)
        {
            var isCompletePage = page.BodyContents.StartsWith("<!DOCTYPE HTML>") || page.BodyContents.StartsWithIgnoreCase("<html");
            if (isCompletePage)
                return null;

            return base.DefaultResolveLayout(page);
        }
    }
}
