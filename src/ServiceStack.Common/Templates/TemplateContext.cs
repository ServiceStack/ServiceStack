using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.IO;
using ServiceStack.Text;

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

        public List<TemplateBlock> TemplateBlocks { get; } = new List<TemplateBlock>();

        public Dictionary<string, Type> CodePages { get; } = new Dictionary<string, Type>();
        
        public HashSet<string> ExcludeFiltersNamed { get; } = new HashSet<string>();

        private readonly Dictionary<string, TemplateBlock> templateBlocksMap = new Dictionary<string, TemplateBlock>(); 
        public TemplateBlock GetBlock(string name) => templateBlocksMap.TryGetValue(name, out var block) ? block : null;

        public ConcurrentDictionary<string, object> Cache { get; } = new ConcurrentDictionary<string, object>();

        public ConcurrentDictionary<ReadOnlyMemory<char>, object> CacheMemory { get; } = new ConcurrentDictionary<ReadOnlyMemory<char>, object>();

        public ConcurrentDictionary<string, Tuple<DateTime, object>> ExpiringCache { get; } = new ConcurrentDictionary<string, Tuple<DateTime, object>>();
        
        public ConcurrentDictionary<ReadOnlyMemory<char>, JsToken> JsTokenCache { get; } = new ConcurrentDictionary<ReadOnlyMemory<char>, JsToken>();

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

        public Func<PageVariableFragment, ReadOnlyMemory<byte>> OnUnhandledExpression { get; set; } = DefaultOnUnhandledExpression;

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

        public void GetPage(string fromVirtualPath, string virtualPath, out TemplatePage page, out TemplateCodePage codePage)
        {
            if (!TryGetPage(fromVirtualPath, virtualPath, out page, out codePage))
                throw new FileNotFoundException($"Page at path was not found: '{virtualPath}'");            
        }
        
        public bool TryGetPage(string fromVirtualPath, string virtualPath, out TemplatePage page, out TemplateCodePage codePage)
        {
            var pathMapKey = nameof(GetPage) + ">" + fromVirtualPath;
            var mappedPath = GetPathMapping(pathMapKey, virtualPath);
            if (mappedPath != null)
            {
                var mappedPage = Pages.GetPage(mappedPath);
                if (mappedPage != null)
                {
                    page = mappedPage;
                    codePage = null;
                    return true;
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
                    return true;
                }

                var p = Pages.GetPage(virtualPath);
                if (p != null)
                {
                    page = p;
                    codePage = null;
                    return true;
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
                    return true;
                }

                var p = Pages.GetPage(seekPath);
                if (p != null)
                {
                    page = p;
                    codePage = null;
                    SetPathMapping(pathMapKey, virtualPath, seekPath);
                    return true;
                }

                if (parentPath == "")
                    break;
                    
                parentPath = parentPath.IndexOf('/') >= 0
                    ? parentPath.LastLeftPart('/')
                    : "";

            } while (true);

            page = null;
            codePage = null;
            return false;
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
            Plugins.Add(new TemplateDefaultBlocks());
            Plugins.Add(new TemplateHtmlBlocks());
            FilterTransformers[TemplateConstants.HtmlEncode] = HtmlPageFormat.HtmlEncodeTransformer;
            FilterTransformers["end"] = stream => (TypeConstants.EmptyByteArray.InMemoryStream() as Stream).InTask();
            FilterTransformers["buffer"] = stream => stream.InTask();
            
            Args[nameof(TemplateConfig.MaxQuota)] = TemplateConfig.MaxQuota;
            Args[nameof(TemplateConfig.DefaultCulture)] = TemplateConfig.CreateCulture();
            Args[nameof(TemplateConfig.DefaultDateFormat)] = TemplateConfig.DefaultDateFormat;
            Args[nameof(TemplateConfig.DefaultDateTimeFormat)] = TemplateConfig.DefaultDateTimeFormat;
            Args[nameof(TemplateConfig.DefaultTimeFormat)] = TemplateConfig.DefaultTimeFormat;
            Args[nameof(TemplateConfig.DefaultFileCacheExpiry)] = TemplateConfig.DefaultFileCacheExpiry;
            Args[nameof(TemplateConfig.DefaultUrlCacheExpiry)] = TemplateConfig.DefaultUrlCacheExpiry;
            Args[nameof(TemplateConfig.DefaultIndent)] = TemplateConfig.DefaultIndent;
            Args[nameof(TemplateConfig.DefaultNewLine)] = TemplateConfig.DefaultNewLine;
            Args[nameof(TemplateConfig.DefaultJsConfig)] = TemplateConfig.DefaultJsConfig;
            Args[nameof(TemplateConfig.DefaultStringComparison)] = TemplateConfig.DefaultStringComparison;
            Args[nameof(TemplateConfig.DefaultTableClassName)] = TemplateConfig.DefaultTableClassName;
            Args[nameof(TemplateConfig.DefaultErrorClassName)] = TemplateConfig.DefaultErrorClassName;
        }

        public TemplateContext RemoveFilters(Predicate<TemplateFilter> match)
        {
            TemplateFilters.RemoveAll(match);
            return this;
        }

        public TemplateContext RemoveBlocks(Predicate<TemplateBlock> match)
        {
            TemplateBlocks.RemoveAll(match);
            return this;
        }

        public TemplateContext RemovePlugins(Predicate<ITemplatePlugin> match)
        {
            Plugins.RemoveAll(match);
            return this;
        }
        
        public Action<TemplateContext> OnAfterPlugins { get; set; }

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
            
            OnAfterPlugins?.Invoke(this);

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

            foreach (var block in TemplateBlocks)
            {
                InitBlock(block);
                templateBlocksMap[block.Name] = block;
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

        internal void InitBlock(TemplateBlock block)
        {
            if (block == null) return;
            if (block.Context == null)
                block.Context = this;
            if (block.Pages == null)
                block.Pages = Pages;
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
                else if (typeof(TemplateBlock).IsAssignableFrom(type))
                {
                    if (TemplateBlocks.All(x => x?.GetType() != type))
                    {
                        Container.AddSingleton(type);
                        var block = (TemplateBlock)Container.Resolve(type);
                        TemplateBlocks.Add(block);
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

        public Action<TemplateScopeContext, object, object> GetAssignExpression(Type targetType, ReadOnlyMemory<char> expression)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));
            if (expression.IsNullOrWhiteSpace())
                throw new ArgumentNullException(nameof(expression));

            var key = targetType.FullName + ':' + expression;

            if (AssignExpressionCache.TryGetValue(key, out var fn))
                return fn;

            AssignExpressionCache[key] = fn = TemplatePageUtils.CompileAssign(targetType, expression);

            return fn;
        }

        protected static ReadOnlyMemory<byte> DefaultOnUnhandledExpression(PageVariableFragment var) => 
            TemplateConfig.HideUnknownExpressions ? null : var.OriginalTextUtf8;

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
