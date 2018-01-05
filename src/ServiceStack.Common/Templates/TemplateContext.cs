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
#if NETSTANDARD2_0
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public class TemplateContext : IDisposable
    {
        public List<PageFormat> PageFormats { get; set; } = new List<PageFormat>();
        
        public string IndexPage { get; set; } = "index";

        public string DefaultLayoutPage { get; set; } = "_layout";

        public ITemplatePages Pages { get; set; }

        public IVirtualPathProvider VirtualFiles { get; set; } = new MemoryVirtualFiles();
        
        public Dictionary<string, object> Args { get; } = new Dictionary<string, object>();

        public bool DebugMode { get; set; } = true;

        public PageFormat GetFormat(string extension) => PageFormats.FirstOrDefault(x => x.Extension == extension);

        public List<Type> ScanTypes { get; set; } = new List<Type>();

        public List<Assembly> ScanAssemblies { get; set; } = new List<Assembly>();

        public IContainer Container { get; set; } = new SimpleContainer();
        
        public IAppSettings AppSettings { get; set; } = new SimpleAppSettings();

        public List<TemplateFilter> TemplateFilters { get; } = new List<TemplateFilter>();

        public Dictionary<string, Type> CodePages { get; } = new Dictionary<string, Type>();
        
        public HashSet<string> ExcludeFiltersNamed { get; } = new HashSet<string>();

        public ConcurrentDictionary<string, object> Cache { get; } = new ConcurrentDictionary<string, object>();

        public ConcurrentDictionary<string, Tuple<DateTime, object>> ExpiringCache { get; } = new ConcurrentDictionary<string, Tuple<DateTime, object>>();

        public ConcurrentDictionary<string, Func<TemplateScopeContext, object, object>> BinderCache { get; } = new ConcurrentDictionary<string, Func<TemplateScopeContext, object, object>>();

        public ConcurrentDictionary<string, Action<TemplateScopeContext, object, object>> AssignExpressionCache { get; } = new ConcurrentDictionary<string, Action<TemplateScopeContext, object, object>>();

        public ConcurrentDictionary<Type, Tuple<MethodInfo, MethodInvoker>> CodePageInvokers { get; } = new ConcurrentDictionary<Type, Tuple<MethodInfo, MethodInvoker>>();

        public ConcurrentDictionary<string, string> PathMappings { get; } = new ConcurrentDictionary<string, string>();
       
        public List<ITemplatePlugin> Plugins { get; } = new List<ITemplatePlugin>();
        
        public HashSet<string> FileFilterNames { get; } = new HashSet<string> { "includeFile", "fileContents" };
        
        /// <summary>
        /// Available transformers that can transform context filter stream outputs
        /// </summary>
        public Dictionary<string, Func<Stream, Task<Stream>>> FilterTransformers { get; set; } = new Dictionary<string, Func<Stream, Task<Stream>>>();

        // Whether to check for modified pages by default when not in DebugMode
        public bool CheckForModifiedPages { get; set; } = false;

        ///How long in between checking for modified pages
        public TimeSpan? CheckForModifiedPagesAfter { get; set; }
        
        /// <summary>
        /// Render render filter exceptions in-line where filter is located
        /// </summary>
        public bool RenderExpressionExceptions { get; set; }

        /// <summary>
        /// What argument to assign Fitler Exceptions to
        /// </summary>
        public string AssignExceptionsTo { get; set; }
        
        /// <summary>
        /// Whether to 
        /// </summary>
        public bool SkipExecutingFiltersIfError { get; set; }

        public Func<PageVariableFragment, byte[]> OnUnhandledExpression { get; set; } = DefaultOnUnhandledExpression;

        public TemplatePage GetPage(string virtualPath)
        {
            var page = Pages.GetPage(virtualPath);
            if (page == null)
                throw new FileNotFoundException($"Page at path was not found: '{virtualPath}'");

            return page;
        }

        public TemplateDefaultFilters DefaultFilters => TemplateFilters.FirstOrDefault(x => x is TemplateDefaultFilters) as TemplateDefaultFilters;
        public TemplateProtectedFilters ProtectedFilters => TemplateFilters.FirstOrDefault(x => x is TemplateProtectedFilters) as TemplateProtectedFilters;
        public TemplateHtmlFilters HtmlFilters => TemplateFilters.FirstOrDefault(x => x is TemplateHtmlFilters) as TemplateHtmlFilters;

        public void TryGetPage(string fromVirtualPath, string virtualPath, out TemplatePage page, out TemplateCodePage codePage)
        {
            var pathMapKey = nameof(TryGetPage) + ">" + fromVirtualPath;
            var mappedPath = GetPathMapping(pathMapKey, virtualPath);
            if (mappedPath != null)
            {
                var mappedPage = Pages.GetPage(mappedPath);
                if (mappedPage != null)
                {
                    page = mappedPage;
                    codePage = null;
                    return;                        
                }
                RemovePathMapping(pathMapKey, mappedPath);
            }

            var tryExactMatch = virtualPath.IndexOf('/') >= 0; //if nested path specified, look for an exact match first
            if (tryExactMatch)
            {
                var cp = GetCodePage(virtualPath);
                if (cp != null)
                {
                    codePage = cp;
                    page = null;
                    return;
                }

                var p = Pages.GetPage(virtualPath);
                if (p != null)
                {
                    page = p;
                    codePage = null;
                    return;
                }
            }
            
            //otherwise find closest match from page.VirtualPath
            var parentPath = fromVirtualPath.IndexOf('/') >= 0
                ? fromVirtualPath.LastLeftPart('/')
                : "";
            do
            {
                var seekPath = parentPath.CombineWith(virtualPath);
                var cp = GetCodePage(seekPath);
                if (cp != null)
                {
                    codePage = cp;
                    page = null;
                    return;
                }

                var p = Pages.GetPage(seekPath);
                if (p != null)
                {
                    page = p;
                    codePage = null;
                    SetPathMapping(pathMapKey, virtualPath, seekPath);
                    return;
                }

                if (parentPath == "")
                    break;
                    
                parentPath = parentPath.IndexOf('/') >= 0
                    ? parentPath.LastLeftPart('/')
                    : "";

            } while (true);
            
            throw new FileNotFoundException($"Page at path was not found: '{virtualPath}'");
        }

        public TemplatePage OneTimePage(string contents, string ext=null) 
            => Pages.OneTimePage(contents, ext ?? PageFormats.First().Extension);

        public TemplateCodePage GetCodePage(string virtualPath)
        {
            var santizePath = virtualPath.Replace('\\','/').TrimPrefixes("/").LastLeftPart('.');

            var isIndexPage = santizePath == string.Empty || santizePath.EndsWith("/");
            var lookupPath = !isIndexPage
                ? santizePath
                : santizePath + IndexPage;
            
            if (!CodePages.TryGetValue(lookupPath, out Type type)) 
                return null;
            
            var instance = (TemplateCodePage) Container.Resolve(type);
            instance.Init();
            return instance;
        }

        public string SetPathMapping(string prefix, string mapPath, string toPath) 
        {
            if (!DebugMode && toPath != null && mapPath != toPath)
                PathMappings[prefix + ">" + mapPath] = toPath;

            return toPath;
        }

        public void RemovePathMapping(string prefix, string mapPath) 
        {
            if (DebugMode)
                return;

            if (mapPath != null)
                PathMappings.TryRemove(prefix + ">" + mapPath, out _);
        }

        public string GetPathMapping(string prefix, string key)
        {
            if (DebugMode)
                return null;

            if (PathMappings.TryGetValue(prefix + ">" + key, out string mappedPath))
                return mappedPath;

            return null;
        }

        public TemplateContext()
        {
            Pages = new TemplatePages(this);
            PageFormats.Add(new HtmlPageFormat());
            TemplateFilters.Add(new TemplateDefaultFilters());
            TemplateFilters.Add(new TemplateHtmlFilters());
            FilterTransformers[TemplateConstants.HtmlEncode] = HtmlPageFormat.HtmlEncodeTransformer;
            FilterTransformers["end"] = stream => (new MemoryStream(TypeConstants.EmptyByteArray) as Stream).InTask();

            var culture = CultureInfo.CurrentCulture;
            if (Equals(culture, CultureInfo.InvariantCulture))
            {
                culture = (CultureInfo) culture.Clone();
                culture.NumberFormat.CurrencySymbol = "$";
            }
            
            Args[TemplateConstants.MaxQuota] = 10000;
            Args[TemplateConstants.DefaultCulture] = culture;
            Args[TemplateConstants.DefaultDateFormat] = "yyyy-MM-dd";
            Args[TemplateConstants.DefaultDateTimeFormat] = "u";
            Args[TemplateConstants.DefaultTimeFormat] = "h\\:mm\\:ss";
            Args[TemplateConstants.DefaultFileCacheExpiry] = TimeSpan.FromMinutes(1);
            Args[TemplateConstants.DefaultUrlCacheExpiry] = TimeSpan.FromMinutes(1);
            Args[TemplateConstants.DefaultIndent] = "\t";
            Args[TemplateConstants.DefaultNewLine] = Environment.NewLine;
            Args[TemplateConstants.DefaultJsConfig] = "excludetypeinfo";
            Args[TemplateConstants.DefaultStringComparison] = StringComparison.Ordinal;
            Args[TemplateConstants.DefaultTableClassName] = "table";
            Args[TemplateConstants.DefaultErrorClassName] = "alert alert-danger";
        }

        public bool HasInit { get; private set; }

        public TemplateContext Init()
        {
            if (HasInit)
                return this;
            HasInit = true;

            Args[TemplateConstants.Debug] = DebugMode;
            
            Container.AddSingleton(() => this);
            Container.AddSingleton(() => Pages);

            var beforePlugins = Plugins.OfType<ITemplatePluginBefore>();
            foreach (var plugin in beforePlugins)
            {
                plugin.BeforePluginsLoaded(this);
            }
            foreach (var plugin in Plugins)
            {
                plugin.Register(this);
            }

            foreach (var type in ScanTypes)
            {
                ScanType(type);
            }

            foreach (var assembly in ScanAssemblies.Safe())
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

            var afterPlugins = Plugins.OfType<ITemplatePluginAfter>();
            foreach (var plugin in afterPlugins)
            {
                plugin.AfterPluginsLoaded(this);
            }

            return this;
        }

        internal void InitFilter(TemplateFilter filter)
        {
            if (filter == null) return;
            if (filter.Context == null)
                filter.Context = this;
            if (filter.Pages == null)
                filter.Pages = Pages;
        }

        public TemplateContext ScanType(Type type)
        {
            if (!type.IsAbstract)
            {
                if (typeof(TemplateFilter).IsAssignableFrom(type))
                {
                    if (TemplateFilters.All(x => x?.GetType() != type))
                    {
                        Container.AddSingleton(type);
                        var filter = (TemplateFilter)Container.Resolve(type);
                        TemplateFilters.Add(filter);
                    }
                }
                else if (typeof(TemplateCodePage).IsAssignableFrom(type))
                {
                    if (CodePages.Values.All(x => x != type))
                    {
                        Container.AddTransient(type);
                        var pageAttr = type.FirstAttribute<PageAttribute>();
                        if (pageAttr?.VirtualPath != null)
                        {
                            CodePages[pageAttr.VirtualPath] = type;
                        }
                    }
                }
            }
            
            return this;
        }

        public Func<TemplateScopeContext, object, object> GetExpressionBinder(Type targetType, StringSegment expression)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));
            if (expression.IsNullOrWhiteSpace())
                throw new ArgumentNullException(nameof(expression));

            var key = targetType.FullName + ':' + expression;

            if (BinderCache.TryGetValue(key, out Func<TemplateScopeContext, object, object> fn))
                return fn;

            BinderCache[key] = fn = TemplatePageUtils.Compile(targetType, expression);

            return fn;
        }
        
        public Action<TemplateScopeContext, object, object> GetAssignExpression(Type targetType, StringSegment expression)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));
            if (expression.IsNullOrWhiteSpace())
                throw new ArgumentNullException(nameof(expression));

            var key = targetType.FullName + ':' + expression;

            if (AssignExpressionCache.TryGetValue(key, out Action<TemplateScopeContext, object, object> fn))
                return fn;

            AssignExpressionCache[key] = fn = TemplatePageUtils.CompileAssign(targetType, expression);

            return fn;
        }

        protected static byte[] DefaultOnUnhandledExpression(PageVariableFragment var) => var.OriginalTextBytes;

        public void Dispose()
        {
            using (Container as IDisposable) {}
        }
    }

    public static class TemplatePagesContextExtensions
    {
        public static string EvaluateTemplate(this TemplateContext context, string template, Dictionary<string, object> args=null)
        {
            var pageResult = new PageResult(context.OneTimePage(template));
            args.Each((x,y) => pageResult.Args[x] = y);
            return pageResult.Result;
        }
        
        public static Task<string> EvaluateTemplateAsync(this TemplateContext context, string template, Dictionary<string, object> args=null)
        {
            var pageResult = new PageResult(context.OneTimePage(template));
            args.Each((x,y) => pageResult.Args[x] = y);
            return pageResult.RenderToStringAsync();
        }
    }
}
