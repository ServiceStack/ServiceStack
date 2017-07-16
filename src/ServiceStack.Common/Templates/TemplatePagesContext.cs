using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
        
        public HashSet<string> ExcludeFiltersNamed { get; } = new HashSet<string>();

        /// <summary>
        /// Available transformers that can transform context filter stream outputs
        /// </summary>
        public Dictionary<string, Func<Stream, Task<Stream>>> FilterTransformers { get; set; } = new Dictionary<string, Func<Stream, Task<Stream>>>();

        public bool CheckForModifiedPages { get; set; } = false;
        
        public bool RenderExpressionExceptions { get; set; }

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
            FilterTransformers["htmlencode"] = HtmlPageFormat.HtmlEncodeTransformer;

            Args[TemplateConstants.DefaultCulture] = CultureInfo.CurrentCulture;
            Args[TemplateConstants.DefaultDateFormat] = "yyyy-MM-dd";
            Args[TemplateConstants.DefaultDateTimeFormat] = "u";
            Args[TemplateConstants.DefaultCacheExpiry] = TimeSpan.FromHours(1);
        }

        public bool HasInit { get; private set; }

        public TemplatePagesContext Init()
        {
            if (HasInit)
                return this;
            HasInit = true;
            
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
            if (filter == null) return;
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

    public static class TemplatePagesContextExtensions
    {
        public static string EvaluateTemplate(this TemplatePagesContext context, string template, Dictionary<string, object> args=null)
        {
            var pageResult = new PageResult(context.OneTimePage(template));
            args.Each((x,y) => pageResult.Args[x] = y);
            return pageResult.Result;
        }
        
        public static Task<string> EvaluateTemplateAsync(this TemplatePagesContext context, string template, Dictionary<string, object> args=null)
        {
            var pageResult = new PageResult(context.OneTimePage(template));
            args.Each((x,y) => pageResult.Args[x] = y);
            return pageResult.RenderToStringAsync();
        }
    }
}
