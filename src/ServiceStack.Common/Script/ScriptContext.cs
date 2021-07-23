using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Configuration;
using ServiceStack.IO;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    public interface IConfigureScriptContext
    {
        void Configure(ScriptContext context);
    }

    public interface IConfigurePageResult
    {
        void Configure(PageResult pageResult);
    }
    
    public partial class ScriptContext : IDisposable
    {
        public List<PageFormat> PageFormats { get; set; } = new();
        
        public string IndexPage { get; set; } = "index";

        public string DefaultLayoutPage { get; set; } = "_layout";

        public ISharpPages Pages { get; set; }

        public IVirtualPathProvider VirtualFiles { get; set; } = new MemoryVirtualFiles();

        /// <summary>
        /// Where to store cached files, if unspecified falls back to configured VirtualFiles if it implements IVirtualFiles (i.e. writable)  
        /// </summary>
        public IVirtualFiles CacheFiles { get; set; }

        public Dictionary<string, object> Args { get; } = new();

        public bool DebugMode { get; set; } = true;

        public PageFormat GetFormat(string extension) => PageFormats.FirstOrDefault(x => x.Extension == extension);

        /// <summary>
        /// Scan Types and auto-register any Script Methods, Blocks and Code Pages
        /// </summary>
        public List<Type> ScanTypes { get; set; } = new();

        /// <summary>
        /// Scan Assemblies and auto-register any Script Methods, Blocks and Code Pages
        /// </summary>
        public List<Assembly> ScanAssemblies { get; set; } = new();

        /// <summary>
        /// Allow scripting of Types from specified Assemblies
        /// </summary>
        public List<Assembly> ScriptAssemblies { get; set; } = new();
        
        /// <summary>
        /// Allow scripting of the specified Types
        /// </summary>
        public List<Type> ScriptTypes { get; set; } = new();
        
        /// <summary>
        /// Lookup Namespaces for resolving Types in Scripts
        /// </summary>
        public List<string> ScriptNamespaces { get; set; } = new();
        
        /// <summary>
        /// Allow scripting of all Types in loaded Assemblies 
        /// </summary>
        public bool AllowScriptingOfAllTypes { get; set; }

        /// <summary>
        /// Register short Type name accessible from scripts. (Advanced, use ScriptAssemblies/ScriptTypes first)
        /// </summary>
        public Dictionary<string, Type> ScriptTypeNameMap { get; } = new(); 
        /// <summary>
        /// Register long qualified Type name accessible from scripts. (Advanced, use ScriptAssemblies/ScriptTypes first)
        /// </summary>
        public Dictionary<string, Type> ScriptTypeQualifiedNameMap { get; } = new(); 

        public IContainer Container { get; set; } = new SimpleContainer();
        
        public IAppSettings AppSettings { get; set; } = new SimpleAppSettings();
        
        public List<Func<string,string>> Preprocessors { get; } = new();
        
        public ScriptLanguage DefaultScriptLanguage { get; set; }

        public List<ScriptLanguage> ScriptLanguages { get; } = new(); 

        internal ScriptLanguage[] ScriptLanguagesArray { get; private set; } 

        public List<ScriptMethods> ScriptMethods { get; } = new();

        /// <summary>
        /// Insert additional Methods at the start so they have priority over default Script Methods   
        /// </summary>
        public List<ScriptMethods> InsertScriptMethods { get; } = new();

        public List<ScriptBlock> ScriptBlocks { get; } = new();

        /// <summary>
        /// Insert additional Blocks at the start so they have priority over default Script Blocks   
        /// </summary>
        public List<ScriptBlock> InsertScriptBlocks { get; } = new();

        public Dictionary<string, Type> CodePages { get; } = new();
        
        public HashSet<string> ExcludeFiltersNamed { get; } = new();

        private readonly Dictionary<string, ScriptLanguage> scriptLanguagesMap = new(); 
        public ScriptLanguage GetScriptLanguage(string name) => scriptLanguagesMap.TryGetValue(name, out var block) ? block : null;

        private readonly Dictionary<string, ScriptBlock> blocksMap = new(); 
        public ScriptBlock GetBlock(string name) => blocksMap.TryGetValue(name, out var block) ? block : null;

        public ConcurrentDictionary<string, object> Cache { get; } = new();

        public ConcurrentDictionary<ReadOnlyMemory<char>, object> CacheMemory { get; } = new();

        public ConcurrentDictionary<string, Tuple<DateTime, object>> ExpiringCache { get; } = new();
        
        public ConcurrentDictionary<ReadOnlyMemory<char>, JsToken> JsTokenCache { get; } = new();

        public ConcurrentDictionary<string, Action<ScriptScopeContext, object, object>> AssignExpressionCache { get; } = new();

        public ConcurrentDictionary<Type, Tuple<MethodInfo, MethodInvoker>> CodePageInvokers { get; } = new();

        public ConcurrentDictionary<string, string> PathMappings { get; } = new();
       
        public List<IScriptPlugin> Plugins { get; } = new();

        /// <summary>
        /// Insert plugins at the start of Plugins so they're registered first
        /// </summary>
        public List<IScriptPlugin> InsertPlugins { get; } = new();
        
        public HashSet<string> FileFilterNames { get; } = new() { "includeFile", "fileContents" };
        
        /// <summary>
        /// Available transformers that can transform context filter stream outputs
        /// </summary>
        public Dictionary<string, Func<Stream, Task<Stream>>> FilterTransformers { get; set; } = new();

        /// <summary>
        /// Whether to check for modified pages by default when not in DebugMode
        /// </summary>
        public bool CheckForModifiedPages { get; set; } = false;

        /// <summary>
        /// How long in between checking for modified pages
        /// </summary>
        public TimeSpan? CheckForModifiedPagesAfter { get; set; }
        
        /// <summary>
        /// Existing caches and pages created prior to specified date should be invalidated 
        /// </summary>
        public DateTime? InvalidateCachesBefore { get; set; }
        
        /// <summary>
        /// Render render filter exceptions in-line where filter is located
        /// </summary>
        public bool RenderExpressionExceptions { get; set; }

        /// <summary>
        /// What argument to assign Exceptions to
        /// </summary>
        public string AssignExceptionsTo { get; set; }
        
        /// <summary>
        /// Whether to skip executing expressions if an Exception was thrown
        /// </summary>
        public bool SkipExecutingFiltersIfError { get; set; }

        /// <summary>
        /// Limit Max Iterations for Heavy Operations like rendering a Script Block (default 10K)
        /// </summary>
        public int MaxQuota { get; set; } = 10000;

        /// <summary>
        /// Limit Max number for micro ops like evaluating an AST instruction (default 1M)
        /// </summary>
        public long MaxEvaluations { get; set; } = 1000000;

        /// <summary>
        /// Limit Recursion Max StackDepth (default 25)
        /// </summary>
        public int MaxStackDepth { get; set; } = 25;

        private ILog log;
        public ILog Log => log ??= LogManager.GetLogger(GetType());
        
        public HashSet<string> RemoveNewLineAfterFiltersNamed { get; set; } = new();
        public HashSet<string> OnlyEvaluateFiltersWhenSkippingPageFilterExecution { get; set; } = new();
        
        public Dictionary<string, ScriptLanguage> ParseAsLanguage { get; set; } = new();
        
        public Func<PageVariableFragment, ReadOnlyMemory<byte>> OnUnhandledExpression { get; set; }
        public Action<PageResult, Exception> OnRenderException { get; set; }

        public SharpPage GetPage(string virtualPath)
        {
            var page = Pages.GetPage(virtualPath);
            if (page == null)
                throw new FileNotFoundException($"Page at path was not found: '{virtualPath}'");

            return page;
        }

        public DefaultScripts DefaultMethods => ScriptMethods.FirstOrDefault(x => x is DefaultScripts) as DefaultScripts;
        public ProtectedScripts ProtectedMethods => ScriptMethods.FirstOrDefault(x => x is ProtectedScripts) as ProtectedScripts;

        public ProtectedScripts AssertProtectedMethods() => ProtectedMethods ?? 
            throw new NotSupportedException("ScriptContext is not configured with ProtectedScripts");
        
        public HtmlScripts HtmlMethods => ScriptMethods.FirstOrDefault(x => x is HtmlScripts) as HtmlScripts;

        public void GetPage(string fromVirtualPath, string virtualPath, out SharpPage page, out SharpCodePage codePage)
        {
            if (!TryGetPage(fromVirtualPath, virtualPath, out page, out codePage))
                throw new FileNotFoundException($"Page at path was not found: '{virtualPath}'");            
        }
        
        public bool TryGetPage(string fromVirtualPath, string virtualPath, out SharpPage page, out SharpCodePage codePage)
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

        private SharpPage emptyPage;
        public SharpPage EmptyPage => emptyPage ??= OneTimePage(""); 

        
        private static InMemoryVirtualFile emptyFile;
        public InMemoryVirtualFile EmptyFile =>
            emptyFile ??= new InMemoryVirtualFile(SharpPages.TempFiles, SharpPages.TempDir) {
                FilePath = "empty", TextContents = ""
            }; 
        
        public SharpPage OneTimePage(string contents, string ext=null) 
            => Pages.OneTimePage(contents, ext ?? PageFormats.First().Extension);

        public SharpCodePage GetCodePage(string virtualPath)
        {
            var sanitizePath = virtualPath.Replace('\\','/').TrimPrefixes("/").LastLeftPart('.');

            var isIndexPage = sanitizePath == string.Empty || sanitizePath.EndsWith("/");
            var lookupPath = !isIndexPage
                ? sanitizePath
                : sanitizePath + IndexPage;
            
            if (!CodePages.TryGetValue(lookupPath, out Type type)) 
                return null;
            
            var instance = (SharpCodePage) Container.Resolve(type);
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

        public ScriptContext()
        {
            Pages = new SharpPages(this);
            PageFormats.Add(new HtmlPageFormat());
            ScriptMethods.Add(new DefaultScripts());
            ScriptMethods.Add(new HtmlScripts());
            Plugins.Add(new DefaultScriptBlocks());
            Plugins.Add(new HtmlScriptBlocks());
            FilterTransformers[ScriptConstants.HtmlEncode] = HtmlPageFormat.HtmlEncodeTransformer;
            FilterTransformers["end"] = stream => (TypeConstants.EmptyByteArray.InMemoryStream() as Stream).InTask();
            FilterTransformers["buffer"] = stream => stream.InTask();
            
            DefaultScriptLanguage = SharpScript.Language;
            ScriptLanguages.Add(ScriptTemplate.Language);
            ScriptLanguages.Add(ScriptCode.Language);
            
            Args[nameof(ScriptConfig.DefaultCulture)] = ScriptConfig.CreateCulture();
            Args[nameof(ScriptConfig.DefaultDateFormat)] = ScriptConfig.DefaultDateFormat;
            Args[nameof(ScriptConfig.DefaultDateTimeFormat)] = ScriptConfig.DefaultDateTimeFormat;
            Args[nameof(ScriptConfig.DefaultTimeFormat)] = ScriptConfig.DefaultTimeFormat;
            Args[nameof(ScriptConfig.DefaultFileCacheExpiry)] = ScriptConfig.DefaultFileCacheExpiry;
            Args[nameof(ScriptConfig.DefaultUrlCacheExpiry)] = ScriptConfig.DefaultUrlCacheExpiry;
            Args[nameof(ScriptConfig.DefaultIndent)] = ScriptConfig.DefaultIndent;
            Args[nameof(ScriptConfig.DefaultNewLine)] = ScriptConfig.DefaultNewLine;
            Args[nameof(ScriptConfig.DefaultJsConfig)] = ScriptConfig.DefaultJsConfig;
            Args[nameof(ScriptConfig.DefaultStringComparison)] = ScriptConfig.DefaultStringComparison;
            Args[nameof(ScriptConfig.DefaultTableClassName)] = ScriptConfig.DefaultTableClassName;
            Args[nameof(ScriptConfig.DefaultErrorClassName)] = ScriptConfig.DefaultErrorClassName;
        }

        public ScriptContext RemoveFilters(Predicate<ScriptMethods> match)
        {
            ScriptMethods.RemoveAll(match);
            return this;
        }

        public ScriptContext RemoveBlocks(Predicate<ScriptBlock> match)
        {
            ScriptBlocks.RemoveAll(match);
            return this;
        }

        public ScriptContext RemovePlugins(Predicate<IScriptPlugin> match)
        {
            Plugins.RemoveAll(match);
            return this;
        }
        
        public Action<ScriptContext> OnAfterPlugins { get; set; }

        public bool HasInit { get; private set; }

        public ScriptContext Init()
        {
            if (HasInit)
                return this;
            HasInit = true;

            if (InsertScriptMethods.Count > 0)
                ScriptMethods.InsertRange(0, InsertScriptMethods);
            if (InsertScriptBlocks.Count > 0)
                ScriptBlocks.InsertRange(0, InsertScriptBlocks);
            if (InsertPlugins.Count > 0)
                Plugins.InsertRange(0, InsertPlugins);
            
            foreach (var assembly in ScanAssemblies.Safe())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(IScriptPlugin).IsAssignableFrom(type))
                    {
                        if (Plugins.All(x => x.GetType() != type))
                        {
                            Container.AddSingleton(type);
                            var plugin = (IScriptPlugin)Container.Resolve(type);
                            Plugins.Add(plugin);
                        }
                    }
                }
            }
            
            Args[ScriptConstants.Debug] = DebugMode;
            
            Container.AddSingleton(() => this);
            Container.AddSingleton(() => Pages);

            ScriptLanguagesArray = ScriptLanguages.Distinct().ToArray();
            foreach (var scriptLanguage in ScriptLanguagesArray)
            {
                scriptLanguagesMap[scriptLanguage.Name] = scriptLanguage;
                
                if (scriptLanguage is IConfigureScriptContext init)
                    init.Configure(this);
            }

            var beforePlugins = Plugins.OfType<IScriptPluginBefore>();
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

            foreach (var method in ScriptMethods)
            {
                InitMethod(method);
            }

            foreach (var block in ScriptBlocks)
            {
                InitBlock(block);
                blocksMap[block.Name] = block;
            }

            ScriptNamespaces = ScriptNamespaces.Distinct().ToList();
            
            var allTypes = new List<Type>(ScriptTypes);
            foreach (var asm in ScriptAssemblies)
            {
                allTypes.AddRange(asm.GetTypes());
            }

            foreach (var type in allTypes)
            {
                if (!ScriptTypeNameMap.ContainsKey(type.Name))
                    ScriptTypeNameMap[type.Name] = type;

                var qualifiedName = ProtectedMethods.typeQualifiedName(type);
                if (!ScriptTypeQualifiedNameMap.ContainsKey(qualifiedName))
                    ScriptTypeQualifiedNameMap[qualifiedName] = type;
            }

            var afterPlugins = Plugins.OfType<IScriptPluginAfter>();
            foreach (var plugin in afterPlugins)
            {
                plugin.AfterPluginsLoaded(this);
            }

            return this;
        }

        internal void InitMethod(ScriptMethods method)
        {
            if (method == null) return;
            if (method.Context == null)
                method.Context = this;
            if (method.Pages == null)
                method.Pages = Pages;

            if (method is IConfigureScriptContext init)
                init.Configure(this);
        }

        internal void InitBlock(ScriptBlock block)
        {
            if (block == null) return;
            if (block.Context == null)
                block.Context = this;
            if (block.Pages == null)
                block.Pages = Pages;

            if (block is IConfigureScriptContext init)
                init.Configure(this);
        }

        public ScriptContext ScanType(Type type)
        {
            if (!type.IsAbstract)
            {
                if (typeof(ScriptMethods).IsAssignableFrom(type))
                {
                    if (ScriptMethods.All(x => x?.GetType() != type))
                    {
                        Container.AddSingleton(type);
                        var method = (ScriptMethods)Container.Resolve(type);
                        ScriptMethods.Add(method);
                    }
                }
                else if (typeof(ScriptBlock).IsAssignableFrom(type))
                {
                    if (ScriptBlocks.All(x => x?.GetType() != type))
                    {
                        Container.AddSingleton(type);
                        var block = (ScriptBlock)Container.Resolve(type);
                        ScriptBlocks.Add(block);
                    }
                }
                else if (typeof(SharpCodePage).IsAssignableFrom(type))
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

        public Action<ScriptScopeContext, object, object> GetAssignExpression(Type targetType, ReadOnlyMemory<char> expression)
        {
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));
            if (expression.IsNullOrWhiteSpace())
                throw new ArgumentNullException(nameof(expression));

            var key = targetType.FullName + ':' + expression;

            if (AssignExpressionCache.TryGetValue(key, out var fn))
                return fn;

            AssignExpressionCache[key] = fn = ScriptTemplateUtils.CompileAssign(targetType, expression);

            return fn;
        }

        public void Dispose()
        {
            using (Container as IDisposable) {}
        }
    }

    public class ReturnValue
    {
        public object Result { get; }
        public Dictionary<string, object> Args { get; }

        public ReturnValue(object result, Dictionary<string, object> args)
        {
            Result = result;
            Args = args;
        }
    }

    public static class ScriptContextUtils
    {
        public static string ErrorNoReturn = "Script did not return a value. Use EvaluateScript() to return script output instead";
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ThrowNoReturn() => throw new NotSupportedException("Script did not return a value");

        public static bool ShouldRethrow(Exception e) =>
            e is ScriptException;

        public static Exception HandleException(Exception e, PageResult pageResult)
        {
            var underlyingEx = e.UnwrapIfSingleException();
            if (underlyingEx is StopFilterExecutionException se)
                underlyingEx = se.InnerException;
            if (underlyingEx is TargetInvocationException te)
                underlyingEx = te.InnerException;
            
#if DEBUG
            var logEx = underlyingEx.GetInnerMostException();
            Logging.LogManager.GetLogger(typeof(ScriptContextUtils)).Error(logEx.Message + "\n" + logEx.StackTrace, logEx);
#endif
            
            if (underlyingEx is ScriptException)
                return underlyingEx;

            pageResult.LastFilterError = underlyingEx;
            return new ScriptException(pageResult);
        }

        public static bool EvaluateResult(this PageResult pageResult, out object returnValue)
        {
            try
            {
                pageResult.WriteToAsync(Stream.Null).Wait();
                if (pageResult.LastFilterError != null)
                    throw new ScriptException(pageResult);

                returnValue = pageResult.ReturnValue?.Result;
                return pageResult.ReturnValue != null;
            }
            catch (Exception e)
            {
                if (ShouldRethrow(e))
                    throw;
                throw HandleException(e, pageResult);
            }
        }

        public static async Task<Tuple<bool, object>> EvaluateResultAsync(this PageResult pageResult)
        {
            try
            {
                await pageResult.WriteToAsync(Stream.Null);
                if (pageResult.LastFilterError != null)
                    throw new ScriptException(pageResult);

                return new Tuple<bool, object>(pageResult.ReturnValue != null, pageResult.ReturnValue?.Result);
            }
            catch (Exception e)
            {
                if (ShouldRethrow(e))
                    throw;
                throw HandleException(e, pageResult);
            }
        }

        public static async Task RenderAsync(this PageResult pageResult, Stream stream, CancellationToken token = default)
        {
            if (pageResult.ResultOutput != null)
            {
                await stream.WriteAsync(MemoryProvider.Instance.ToUtf8Bytes(pageResult.ResultOutput.AsSpan()), token: token);
                return;
            }

            await pageResult.Init();
            await pageResult.WriteToAsync(stream, token);
            if (pageResult.LastFilterError != null)
                throw new ScriptException(pageResult);
        }

        public static void RenderToStream(this PageResult pageResult, Stream stream)
        {
            try 
            { 
                try
                {
                    if (pageResult.ResultOutput != null)
                    {
                        if (pageResult.LastFilterError != null)
                            throw new ScriptException(pageResult);

                        stream.WriteAsync(MemoryProvider.Instance.ToUtf8Bytes(pageResult.ResultOutput.AsSpan())).Wait();
                        return;
                    }

                    pageResult.Init().Wait();
                    pageResult.WriteToAsync(stream).Wait();
                    if (pageResult.LastFilterError != null)
                        throw new ScriptException(pageResult);
                }
                catch (AggregateException e)
                {
                    var ex = e.UnwrapIfSingleException();
                    throw ex;
                }
            }
            catch (Exception e)
            {
                if (ShouldRethrow(e))
                    throw;
                throw HandleException(e, pageResult);
            }
        }

        public static async Task RenderToStreamAsync(this PageResult pageResult, Stream stream)
        {
            try 
            { 
                if (pageResult.ResultOutput != null)
                {
                    if (pageResult.LastFilterError != null)
                        throw new ScriptException(pageResult);

                    await stream.WriteAsync(MemoryProvider.Instance.ToUtf8Bytes(pageResult.ResultOutput.AsSpan()));
                    return;
                }

                await pageResult.Init();
                await pageResult.WriteToAsync(stream);
                if (pageResult.LastFilterError != null)
                    throw new ScriptException(pageResult);
            }
            catch (Exception e)
            {
                if (ShouldRethrow(e))
                    throw;
                throw HandleException(e, pageResult);
            }
        }

        public static string RenderScript(this PageResult pageResult)
        {
            try
            {
                using var ms = MemoryStreamFactory.GetStream();
                pageResult.RenderToStream(ms);
                var output = ms.ReadToEnd();
                return output;
            }
            catch (Exception e)
            {
                if (ShouldRethrow(e))
                    throw;
                throw HandleException(e, pageResult);
            }
        }

        public static async Task<string> RenderScriptAsync(this PageResult pageResult, CancellationToken token = default)
        {
            try
            {
                using var ms = MemoryStreamFactory.GetStream();
                await RenderAsync(pageResult, ms, token);
                var output = await ms.ReadToEndAsync();
                return output;
            }
            catch (Exception e)
            {
                if (ShouldRethrow(e))
                    throw;
                throw HandleException(e, pageResult);
            }
        }

        public static ScriptScopeContext CreateScope(this ScriptContext context, Dictionary<string, object> args = null, 
            ScriptMethods functions = null, ScriptBlock blocks = null)
        {
            var pageContext = new PageResult(context.EmptyPage);
            if (functions != null)
                pageContext.ScriptMethods.Insert(0, functions);
            if (blocks != null)
                pageContext.ScriptBlocks.Insert(0, blocks);

            return new ScriptScopeContext(pageContext, null, args);
        }
    }
}
