using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
    public class TemplatePagesContext : IDisposable
    {
        public List<PageFormat> PageFormats { get; set; } = new List<PageFormat>();
        
        public string IndexPage { get; set; } = "index";

        public string DefaultLayoutPage { get; set; } = "_layout";
        
        public string LayoutVarName { get; set; } = "layout";

        public bool CheckModifiedPages { get; set; } = false;

        public ITemplatePages Pages { get; set; }

        public IVirtualPathProvider VirtualFiles { get; set; } = new MemoryVirtualFiles();
        
        public Dictionary<string, object> Args { get; } = new Dictionary<string, object>();

        public bool DebugMode { get; set; } = true;

        public PageFormat GetFormat(string extension) => PageFormats.FirstOrDefault(x => x.Extension == extension);

        public List<Type> ScanTypes { get; set; } = new List<Type>();

        public List<Assembly> ScanAssemblies{ get; set; } = new List<Assembly>();

        public IContainer Container { get; set; } = new SimpleContainer();
        
        public IAppSettings AppSettings { get; set; } = new SimpleAppSettings();

        public List<TemplateFilter> TemplateFilters { get; } = new List<TemplateFilter>();

        public List<TemplateCode> CodePages { get; } = new List<TemplateCode>();

        public TemplatePage GetPage(string virtualPath)
        {
            var page = Pages.GetPage(virtualPath);
            if (page == null)
                throw new FileNotFoundException($"Page at path was not found: '{virtualPath}'");

            return page;
        }

        public TemplatePage OneTimePage(string contents, string ext=null) => Pages.OneTimePage(contents, ext ?? PageFormats.First().Extension);

        public TemplatePagesContext()
        {
            Pages = new TemplatePages(this);
            PageFormats.Add(new HtmlPageFormat());
            TemplateFilters.Add(new TemplateDefaultFilters());

            Args[TemplateConstants.DefaultCulture] = CultureInfo.CurrentCulture;
            Args[TemplateConstants.DefaultDateFormat] = "yyyy-MM-dd";
            Args[TemplateConstants.DefaultDateTimeFormat] = "u";
        }

        public TemplatePagesContext Init()
        {
            Container.AddSingleton(() => this);
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
                InitFilter(filter);
            }

            foreach (var page in CodePages)
            {
                InitCodePage(page);
            }

            return this;
        }

        internal void InitCodePage(TemplateCode page)
        {
            if (page.Context == null)
                page.Context = this;
            if (page.Pages == null)
                page.Pages = Pages;

            page.Init();
        }

        internal void InitFilter(TemplateFilter filter)
        {
            if (filter.Context == null)
                filter.Context = this;
            if (filter.Pages == null)
                filter.Pages = Pages;
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

        private readonly ConcurrentDictionary<string, Func<object, object>> binderCache = new ConcurrentDictionary<string, Func<object, object>>();

        public Func<object, object> GetExpressionBinder(Type targetType, StringSegment expression)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));
            if (expression.IsNullOrWhiteSpace())
                throw new ArgumentNullException(nameof(expression));

            var key = $"{targetType.FullName}::{expression}";

            if (binderCache.TryGetValue(key, out Func<object, object> fn))
                return fn;

            binderCache[key] = fn = TemplatePageUtils.Compile(targetType, expression);

            return fn;
        }

        public void Dispose()
        {
            using (Container as IDisposable) {}
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
            page.Args.TryGetValue(TemplatePages.Layout, out object layout);
            return page.Context.Pages.ResolveLayoutPage(page, layout as string);
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
