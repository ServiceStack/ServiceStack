using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Logging;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Script;

public interface IPageResult {}

// Render a Template Page to the Response OutputStream
public class PageResult : IPageResult, IStreamWriterAsync, IHasOptions, IDisposable
{
    /// <summary>
    /// The Page to Render
    /// </summary>
    public SharpPage Page { get; }
        
    /// <summary>
    /// The Code Page to Render
    /// </summary>
    public SharpCodePage CodePage { get; }
        
    /// <summary>
    /// Use specified Layout 
    /// </summary>
    public SharpPage LayoutPage { get; set; }
        
    /// <summary>
    /// Use Layout with specified name
    /// </summary>
    public string Layout { get; set; }
        
    /// <summary>
    /// Render without any Layout
    /// </summary>
    public bool NoLayout { get; set; }

    /// <summary>
    /// Extract Model Properties into Scope Args
    /// </summary>
    public object Model { get; set; }

    /// <summary>
    /// Add additional Args available to all pages 
    /// </summary>
    public Dictionary<string, object> Args { get; set; }

    /// <summary>
    /// Add additional script methods available to all pages
    /// </summary>
    public List<ScriptMethods> ScriptMethods { get; set; }

    [Obsolete("Use ScriptMethods")] public List<ScriptMethods> TemplateFilters => ScriptMethods;

    /// <summary>
    /// Add additional script blocks available to all pages
    /// </summary>
    public List<ScriptBlock> ScriptBlocks { get; set; }

    [Obsolete("Use ScriptBlocks")] public List<ScriptBlock> TemplateBlocks => ScriptBlocks;
        
    /// <summary>
    /// Add additional partials available to all pages
    /// </summary>
    public Dictionary<string, SharpPage> Partials { get; set; }

    /// <summary>
    /// Return additional HTTP Headers in HTTP Requests
    /// </summary>
    public IDictionary<string, string> Options { get; set; }

    /// <summary>
    /// Specify the Content-Type of the Response 
    /// </summary>
    public string ContentType
    {
        get => Options.TryGetValue(HttpHeaders.ContentType, out string contentType) ? contentType : null;
        set => Options[HttpHeaders.ContentType] = value;
    }

    /// <summary>
    /// Transform the Page output using a chain of stream transformers
    /// </summary>
    public List<Func<Stream, Task<Stream>>> PageTransformers { get; set; }

    /// <summary>
    /// Transform the entire output using a chain of stream transformers
    /// </summary>
    public List<Func<Stream, Task<Stream>>> OutputTransformers { get; set; }

    /// <summary>
    /// Available transformers that can transform context filter stream outputs
    /// </summary>
    public Dictionary<string, Func<Stream, Task<Stream>>> FilterTransformers { get; set; }
        
    /// <summary>
    /// Don't allow access to specified filters
    /// </summary>
    public HashSet<string> ExcludeFiltersNamed { get; } = new HashSet<string>();

    /// <summary>
    /// The last error thrown by a filter
    /// </summary>
    public Exception LastFilterError { get; set; }
        
    /// <summary>
    /// The StackTrace where the Last Error Occured 
    /// </summary>
    public string[] LastFilterStackTrace { get; set; }
        
    /// <summary>
    /// What argument errors should be binded to
    /// </summary>
    public string AssignExceptionsTo { get; set; }

    /// <summary>
    /// What argument captured errors should be binded to
    /// </summary>
    public string CatchExceptionsIn { get; set; }

    /// <summary>
    /// Whether to skip execution of all page filters and just write page string fragments
    /// </summary>
    public bool SkipFilterExecution { get; set; }

    /// <summary>
    /// Overrides Context to specify whether to Ignore or Continue executing filters on error 
    /// </summary>
    public bool? SkipExecutingFiltersIfError { get; set; }
        
    /// <summary>
    /// Whether to always rethrow Exceptions
    /// </summary>
    public bool RethrowExceptions { get; set; }

    /// <summary>
    /// Immediately halt execution of the page
    /// </summary>
    public bool HaltExecution { get; set; }

    /// <summary>
    /// Whether to disable buffering output and render directly to OutputStream
    /// </summary>
    public bool DisableBuffering { get; set; }
        
    /// <summary>
    /// The Return value of the page (if any)
    /// </summary>
    public ReturnValue ReturnValue { get; set; }
        
    /// <summary>
    /// The Current StackDepth
    /// </summary>
    public int StackDepth { get; internal set; }
        
    /// <summary>
    /// The Current StackDepth of rendering partials
    /// </summary>
    public int PartialStackDepth { get; internal set; }

    /// <summary>
    /// Can be used to track number of Evaluations
    /// </summary>
    public long Evaluations { get; private set; }
        
    /// <summary>
    /// Can be used to track number of Evaluations
    /// </summary>
    internal bool PageProcessed { get; set; }
        
    public void AssertNextEvaluation()
    {
        if (Evaluations++ >= Context.MaxEvaluations)
            throw new NotSupportedException($"Exceeded Max Evaluations of {Context.MaxEvaluations}. \nMaxEvaluations can be changed in `ScriptContext.MaxEvaluations`.");
    }
        
    public void AssertNextPartial()
    {
        if (PartialStackDepth++ >= Context.MaxStackDepth)
            throw new NotSupportedException($"Exceeded Max Partial StackDepth of {Context.MaxStackDepth}. \nMaxStackDepth can be changed in `ScriptContext.MaxStackDepth`.");
    }

    public void ResetIterations() => Evaluations = 0;
        
    private readonly Stack<string> stackTrace = new Stack<string>();

    private PageResult(PageFormat format)
    {
        Args = new Dictionary<string, object>();
        ScriptMethods = new List<ScriptMethods>();
        ScriptBlocks = new List<ScriptBlock>();
        Partials = new Dictionary<string, SharpPage>();
        PageTransformers = new List<Func<Stream, Task<Stream>>>();
        OutputTransformers = new List<Func<Stream, Task<Stream>>>();
        FilterTransformers = new Dictionary<string, Func<Stream, Task<Stream>>>();
        Options = new Dictionary<string, string>
        {
            {HttpHeaders.ContentType, format?.ContentType},
        };
    }

    public PageResult(SharpPage page) : this(page?.Format)
    {
        Page = page ?? throw new ArgumentNullException(nameof(page));
    }

    public PageResult(SharpCodePage page) : this(page?.Format)
    {
        CodePage = page ?? throw new ArgumentNullException(nameof(page));

        var hasRequest = (CodePage as IRequiresRequest)?.Request;
        if (hasRequest != null)
            Args[ScriptConstants.Request] = hasRequest;
    }

    public PageResult AssignArgs(Dictionary<string, object> args)
    {
        if (args != null)
        {
            foreach (var entry in args)
            {
                Args[entry.Key] = entry.Value;
            }
        }
        return this;
    }

    //entry point
    public async Task WriteToAsync(Stream responseStream, CancellationToken token = default)
    {
        if (OutputTransformers.Count == 0)
        {
            var bufferOutput = !DisableBuffering && !(responseStream is MemoryStream);
            if (bufferOutput)
            {
                using var ms = MemoryStreamFactory.GetStream();
                await WriteToAsyncInternal(ms, token).ConfigAwait();
                ms.Position = 0;
                await ms.WriteToAsync(responseStream, token).ConfigAwait();
            }
            else
            {
                await WriteToAsyncInternal(responseStream, token).ConfigAwait();
            }
            return;
        }

        //If PageResult has any OutputFilters Buffer and chain stream responses to each
        using (var ms = MemoryStreamFactory.GetStream())
        {
            stackTrace.Push("OutputTransformer");
                
            await WriteToAsyncInternal(ms, token).ConfigAwait();
            Stream stream = ms;

            foreach (var transformer in OutputTransformers)
            {
                stream.Position = 0;
                stream = await transformer(stream).ConfigAwait();
            }

            using (stream)
            {
                stream.Position = 0;
                await stream.WriteToAsync(responseStream, token).ConfigAwait();
            }

            stackTrace.Pop();
        }
    }

    internal async Task WriteToAsyncInternal(Stream outputStream, CancellationToken token)
    {
        await Init().ConfigAwait();

        if (!NoLayout)
        {
            if (LayoutPage != null)
            {
                await LayoutPage.Init().ConfigAwait();
                
                if (CodePage != null)
                    InitIfNewPage(CodePage);

                if (Page != null)
                    await InitIfNewPage(Page).ConfigAwait();
            }
            else
            {
                if (Page != null)
                {
                    await InitIfNewPage(Page).ConfigAwait();
                    if (Page.LayoutPage != null)
                    {
                        LayoutPage = Page.LayoutPage;
                        await LayoutPage.Init().ConfigAwait();
                    }
                }
                else if (CodePage != null)
                {
                    InitIfNewPage(CodePage);
                    if (CodePage.LayoutPage != null)
                    {
                        LayoutPage = CodePage.LayoutPage;
                        await LayoutPage.Init().ConfigAwait();
                    }
                }
            }
        }
        else
        {
            if (Page != null)
            {
                await InitIfNewPage(Page).ConfigAwait();
            }
            else if (CodePage != null)
            {
                InitIfNewPage(CodePage);
            }
        }

        token.ThrowIfCancellationRequested();

        var pageScope = CreatePageContext(null, outputStream);

        if (!NoLayout && LayoutPage != null)
        {
            // sync impl with WriteFragmentsAsync
            stackTrace.Push("Layout: " + LayoutPage.VirtualPath);
                
            foreach (var fragment in LayoutPage.PageFragments)
            {
                if (HaltExecution)
                    break;

                await WritePageFragmentAsync(pageScope, fragment, token).ConfigAwait();
            }

            stackTrace.Pop();
        }
        else
        {
            await WritePageAsync(Page, CodePage, pageScope, token).ConfigAwait();
        }
    }

    internal async Task WriteFragmentsAsync(ScriptScopeContext scope, IEnumerable<PageFragment> fragments, string callTrace, CancellationToken token)
    {
        stackTrace.Push(callTrace);

        foreach (var fragment in fragments)
        {
            if (HaltExecution)
                return;

            await WritePageFragmentAsync(scope, fragment, token).ConfigAwait();
        }
            
        stackTrace.Pop();
    }

    public async Task WritePageFragmentAsync(ScriptScopeContext scope, PageFragment fragment, CancellationToken token)
    {
        foreach (var scriptLanguage in Context.ScriptLanguagesArray)
        {
            if (ShouldSkipFilterExecution(fragment))
                return;
                
            if (await scriptLanguage.WritePageFragmentAsync(scope, fragment, token).ConfigAwait())
                break;
        }
    }

    public Task WriteStatementsAsync(ScriptScopeContext scope, IEnumerable<JsStatement> blockStatements, string callTrace, CancellationToken token)
    {
        try
        {
            stackTrace.Push(callTrace);
            return WriteStatementsAsync(scope, blockStatements, token);
        }
        finally
        {
                
            stackTrace.Pop();
        }
    }
        
    public async Task WriteStatementsAsync(ScriptScopeContext scope, IEnumerable<JsStatement> blockStatements, CancellationToken token)
    {
        foreach (var statement in blockStatements)
        {
            foreach (var scriptLanguage in Context.ScriptLanguagesArray)
            {
                if (HaltExecution || ShouldSkipFilterExecution(statement))
                    return;

                if (await scriptLanguage.WriteStatementAsync(scope, statement, token).ConfigAwait())
                    break;
            }
        }
    }

    public bool ShouldSkipFilterExecution(PageVariableFragment var)
    {
        return HaltExecution || SkipFilterExecution && (var.Binding != null 
            ? !Context.OnlyEvaluateFiltersWhenSkippingPageFilterExecution.Contains(var.Binding)
            : var.InitialExpression?.Name == null || 
              !Context.OnlyEvaluateFiltersWhenSkippingPageFilterExecution.Contains(var.InitialExpression.Name));
    }

    public bool ShouldSkipFilterExecution(PageFragment fragment) => !(fragment is PageStringFragment) 
                                                                    && (fragment is PageVariableFragment var 
                                                                        ? ShouldSkipFilterExecution(var)
                                                                        : HaltExecution || SkipFilterExecution);

    public bool ShouldSkipFilterExecution(JsStatement statement) => HaltExecution || SkipFilterExecution; 

    public ScriptContext Context => Page?.Context ?? CodePage.Context;
    public PageFormat Format => Page?.Format ?? CodePage.Format;
    public string VirtualPath => Page?.VirtualPath ?? CodePage.VirtualPath;

    private bool hasInit;
    public async Task<PageResult> Init()
    {
        if (hasInit)
            return this;

        if (!Context.HasInit)
            throw new NotSupportedException($"{Context.GetType().Name} has not been initialized. Call 'Init()' to initialize Script Context.");

        if (Model != null)
        {
            var explodeModel = Model.ToObjectDictionary();
            foreach (var entry in explodeModel)
            {
                Args[entry.Key] = entry.Value ?? JsNull.Value;
            }
        }
        Args[ScriptConstants.Model] = Model ?? JsNull.Value;

        foreach (var scriptLanguage in Context.ScriptLanguages)
        {
            if (scriptLanguage is IConfigurePageResult configurePageResult)
            {
                configurePageResult.Configure(this);
            }
        }

        foreach (var filter in ScriptMethods)
        {
            Context.InitMethod(filter);
        }

        foreach (var block in ScriptBlocks)
        {
            Context.InitBlock(block);
            blocksMap[block.Name] = block;
        }

        if (Page != null)
        {
            await Page.Init().ConfigAwait();
            InitPageArgs(Page.Args);
        }
        else
        {
            CodePage.Init();
            InitPageArgs(CodePage.Args);
        }

        if (Layout != null && !NoLayout)
        {
            LayoutPage = Page != null
                ? Context.Pages.ResolveLayoutPage(Page, Layout)
                : Context.Pages.ResolveLayoutPage(CodePage, Layout);
        }
            
        hasInit = true;

        return this;
    }

    private void InitPageArgs(Dictionary<string, object> pageArgs)
    {
        if (pageArgs?.Count > 0)
        {
            NoLayout = (pageArgs.TryGetValue("ignore", out object ignore) && "template".Equals(ignore?.ToString())) ||
                       (pageArgs.TryGetValue("layout", out object layout) && "none".Equals(layout?.ToString()));
        }
    }

    private Task InitIfNewPage(SharpPage page) => page != Page 
        ? (Task) page.Init() 
        : TypeConstants.EmptyTask;

    private void InitIfNewPage(SharpCodePage page)
    {
        if (page != CodePage)
            page.Init();
    }

    private void AssertInit()
    {
        if (!hasInit)
            throw new NotSupportedException("PageResult.Init() required for this operation.");
    }

    public Task WritePageAsync(SharpPage page, SharpCodePage codePage,
        ScriptScopeContext scope, CancellationToken token = default)
    {
        try
        {
            AssertNextPartial();
                
            if (page != null)
                return WritePageAsync(page, scope, token);

            return WriteCodePageAsync(codePage, scope, token);
        }
        finally
        {
            PartialStackDepth--;
        }
    }

    public async Task WritePageAsync(SharpPage page, ScriptScopeContext scope, CancellationToken token = default)
    {
        if (PageTransformers.Count == 0)
        {
            await WritePageAsyncInternal(page, scope, token).ConfigAwait();
            return;
        }

        //If PageResult has any PageFilters Buffer and chain stream responses to each
        using var ms = MemoryStreamFactory.GetStream();
        stackTrace.Push("PageTransformer");

        await WritePageAsyncInternal(page, new ScriptScopeContext(this, ms, scope.ScopedParams), token).ConfigAwait();
        Stream stream = ms;

        foreach (var transformer in PageTransformers)
        {
            stream.Position = 0;
            stream = await transformer(stream).ConfigAwait();
        }

        using (stream)
        {
            stream.Position = 0;
            await stream.WriteToAsync(scope.OutputStream, token).ConfigAwait();
        }

        stackTrace.Pop();
    }

    internal async Task WritePageAsyncInternal(SharpPage page, ScriptScopeContext scope, CancellationToken token = default(CancellationToken))
    {
        await page.Init().ConfigAwait(); //reload modified changes if needed

        await WriteFragmentsAsync(scope, page.PageFragments, "Page: " + page.VirtualPath, token).ConfigAwait();
    }

    public async Task WriteCodePageAsync(SharpCodePage page, ScriptScopeContext scope, CancellationToken token = default(CancellationToken))
    {
        if (PageTransformers.Count == 0)
        {
            await WriteCodePageAsyncInternal(page, scope, token).ConfigAwait();
            return;
        }

        //If PageResult has any PageFilters Buffer and chain stream responses to each
        using var ms = MemoryStreamFactory.GetStream();
        await WriteCodePageAsyncInternal(page, new ScriptScopeContext(this, ms, scope.ScopedParams), token).ConfigAwait();
        Stream stream = ms;

        foreach (var transformer in PageTransformers)
        {
            stream.Position = 0;
            stream = await transformer(stream).ConfigAwait();
        }

        using (stream)
        {
            stream.Position = 0;
            await stream.WriteToAsync(scope.OutputStream, token).ConfigAwait();
        }
    }

    internal Task WriteCodePageAsyncInternal(SharpCodePage page, ScriptScopeContext scope, CancellationToken token = default(CancellationToken))
    {
        page.Scope = scope;

        if (!page.HasInit)
            page.Init();

        return page.WriteAsync(scope);
    }

    private string toDebugString(object instance)
    {
        using (JsConfig.With(new Config
               {
                   ExcludeTypeInfo = true,
                   IncludeTypeInfo = false,
               }))
        {
            if (instance is Dictionary<string, object> d)
                return d.ToJsv();
            if (instance is List<object> l)
                return l.ToJsv();
            if (instance is string s)
                return '"' + s.Replace("\"", "\\\"") + '"';
            return instance.ToJsv();
        }
    }

    public async Task WriteVarAsync(ScriptScopeContext scope, PageVariableFragment var, CancellationToken token)
    {
        if (var.Binding != null)
            stackTrace.Push($"Expression (binding): " + var.Binding);
        else if (var.InitialExpression?.Name != null)
            stackTrace.Push("Expression (filter): " + var.InitialExpression.Name);
        else if (var.InitialValue != null)
            stackTrace.Push($"Expression ({var.InitialValue.GetType().Name}): " + toDebugString(var.InitialValue).SubstringWithEllipsis(0, 200));
        else 
            stackTrace.Push($"{var.Expression.GetType().Name}: " + var.Expression.ToRawString().SubstringWithEllipsis(0, 200));
            
        var value = await EvaluateAsync(var, scope, token).ConfigAwait();
        if (value != IgnoreResult.Value)
        {
            if (value != null)
            {
                var bytes = Format.EncodeValue(value).ToUtf8Bytes();
                await scope.OutputStream.WriteAsync(bytes, token).ConfigAwait();
            }
            else
            {
                if (Context.OnUnhandledExpression != null)
                {
                    var bytes = Context.OnUnhandledExpression(var);
                    if (bytes.Length > 0)
                        await scope.OutputStream.WriteAsync(bytes, token).ConfigAwait();
                }
            }
        }

        stackTrace.Pop();
    }

    private Func<Stream, Task<Stream>> GetFilterTransformer(string name)
    {
        return FilterTransformers.TryGetValue(name, out Func<Stream, Task<Stream>> fn)
            ? fn
            : Context.FilterTransformers.TryGetValue(name, out fn)
                ? fn
                : null;
    }

    private static Dictionary<string, object> GetPageParams(PageVariableFragment var)
    {
        Dictionary<string, object> scopedParams = null;
        if (var != null && var.FilterExpressions.Length > 0)
        {
            if (var.FilterExpressions[0].Arguments.Length > 0)
            {
                var token = var.FilterExpressions[0].Arguments[0];
                scopedParams = token.Evaluate(JS.CreateScope()) as Dictionary<string, object>;
            }
        }
        return scopedParams;
    }

    private ScriptScopeContext CreatePageContext(PageVariableFragment var, Stream outputStream) => new ScriptScopeContext(this, outputStream, GetPageParams(var));

    private async Task<object> EvaluateAsync(PageVariableFragment var, ScriptScopeContext scope, CancellationToken token=default(CancellationToken))
    {
        scope.ScopedParams[nameof(PageVariableFragment)] = var;

        var value = var.Evaluate(scope);
        if (value == null)
        {
            var handlesUnknownValue = Context.OnUnhandledExpression == null &&
                                      var.FilterExpressions.Length > 0;
                
            if (!handlesUnknownValue)
            {
                if (var.Expression is JsMemberExpression memberExpr)
                {
                    //allow nested null bindings from an existing target to evaluate to an empty string 
                    var targetValue = memberExpr.Object.Evaluate(scope);
                    if (targetValue != null)
                        return string.Empty;
                }

                if (var.Binding == null)
                    return null;

                var hasFilterAsBinding = GetFilterAsBinding(var.Binding, out ScriptMethods filter);
                if (hasFilterAsBinding != null)
                {
                    value = InvokeFilter(hasFilterAsBinding, filter, new object[0], var.Binding);
                }
                else
                {
                    var hasContextFilterAsBinding = GetContextFilterAsBinding(var.Binding, out filter);
                    if (hasContextFilterAsBinding != null)
                    {
                        value = InvokeFilter(hasContextFilterAsBinding, filter, new object[] { scope }, var.Binding);
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        if (value == JsNull.Value)
            value = null;

        value = EvaluateIfToken(value, scope);
            
        for (var i = 0; i < var.FilterExpressions.Length; i++)
        {
            if (HaltExecution || value == StopExecution.Value)
                break;

            var expr = var.FilterExpressions[i];

            try
            {
                var filterName = expr.Name;

                var fnArgValues = JsCallExpression.EvaluateArgumentValues(scope, expr.Arguments);
                var fnArgsLength = fnArgValues.Count;

                var invoker = GetFilterInvoker(filterName, 1 + fnArgsLength, out ScriptMethods filter);
                var contextFilterInvoker = invoker == null
                    ? GetContextFilterInvoker(filterName, 2 + fnArgsLength, out filter)
                    : null;
                var contextBlockInvoker = invoker == null && contextFilterInvoker == null
                    ? GetContextBlockInvoker(filterName, 2 + fnArgsLength, out filter)
                    : null;
                var delegateInvoker = invoker == null && contextFilterInvoker == null && contextBlockInvoker == null
                    ? GetValue(filterName, scope) as Delegate
                    : null;

                if (invoker == null && contextFilterInvoker == null && contextBlockInvoker == null && delegateInvoker == null)
                {
                    if (i == 0)
                        return null; // ignore on server (i.e. assume it's on client) if first filter is missing  

                    var errorMsg = CreateMissingFilterErrorMessage(filterName);
                    throw new NotSupportedException(errorMsg);
                }

                if (value is Task<object> valueObjectTask)
                    value = await valueObjectTask.ConfigAwait();

                if (delegateInvoker != null)
                {
                    value = JsCallExpression.InvokeDelegate(delegateInvoker, value, true, fnArgValues);
                }
                else if (invoker != null)
                {
                    fnArgValues.Insert(0, value);
                    var args = fnArgValues.ToArray();

                    value = InvokeFilter(invoker, filter, args, expr.Name);
                }
                else if (contextFilterInvoker != null)
                {
                    fnArgValues.Insert(0, scope);
                    fnArgValues.Insert(1, value); // filter target
                    var args = fnArgValues.ToArray();

                    value = InvokeFilter(contextFilterInvoker, filter, args, expr.Name);
                }
                else
                {
                    var hasFilterTransformers = var.FilterExpressions.Length + i > 1;

                    var useScope = hasFilterTransformers
                        ? scope.ScopeWithStream(MemoryStreamFactory.GetStream())
                        : scope;

                    fnArgValues.Insert(0, useScope);
                    fnArgValues.Insert(1, value); // filter target
                    var args = fnArgValues.ToArray();

                    try
                    {
                        var taskResponse = (Task)contextBlockInvoker(filter, args);
                        await taskResponse.ConfigAwait();

                        if (hasFilterTransformers)
                        {
                            using (useScope.OutputStream)
                            {
                                var stream = useScope.OutputStream;

                                //If Context Filter has any Filter Transformers Buffer and chain stream responses to each
                                for (var exprIndex = i + 1; exprIndex < var.FilterExpressions.Length; exprIndex++)
                                {
                                    stream.Position = 0;

                                    contextBlockInvoker = GetContextBlockInvoker(var.FilterExpressions[exprIndex].Name, 1 + var.FilterExpressions[exprIndex].Arguments.Length, out filter);
                                    if (contextBlockInvoker != null)
                                    {
                                        args[0] = useScope;
                                        for (var cmdIndex = 0; cmdIndex < var.FilterExpressions[exprIndex].Arguments.Length; cmdIndex++)
                                        {
                                            var arg = var.FilterExpressions[exprIndex].Arguments[cmdIndex];
                                            var varValue = arg.Evaluate(scope);
                                            args[1 + cmdIndex] = varValue;
                                        }

                                        await ((Task) contextBlockInvoker(filter, args)).ConfigAwait();
                                    }
                                    else
                                    {
                                        var transformer = GetFilterTransformer(var.FilterExpressions[exprIndex].Name);
                                        if (transformer == null)
                                            throw new NotSupportedException($"Could not find FilterTransformer '{var.FilterExpressions[exprIndex].Name}' in page '{Page.VirtualPath}'");

                                        stream = await transformer(stream).ConfigAwait();
                                        useScope = useScope.ScopeWithStream(stream);
                                    }
                                }

                                if (stream.CanRead)
                                {
                                    stream.Position = 0;
                                    await stream.WriteToAsync(scope.OutputStream, token).ConfigAwait();
                                }
                            }
                        }
                    }
                    catch (StopFilterExecutionException) { throw; }
                    catch (Exception ex)
                    {
                        var rethrow = ScriptConfig.FatalExceptions.Contains(ex.GetType());

                        var exResult = Format.OnExpressionException(this, ex);
                        if (exResult != null)
                            await scope.OutputStream.WriteAsync(Format.EncodeValue(exResult).ToUtf8Bytes(), token).ConfigAwait();
                        else if (rethrow)
                            throw;

                        throw new TargetInvocationException($"Failed to invoke script method '{expr.GetDisplayName()}': {ex.Message}", ex);
                    }

                    return IgnoreResult.Value;
                }

                if (value is Task<object> valueTask)
                    value = await valueTask.ConfigAwait();
            }
            catch (Exception ex)
            {
                var stopEx = ex as StopFilterExecutionException;
                var useEx = stopEx?.InnerException ?? ex;
                    
                LastFilterError = useEx;
                LastFilterStackTrace = stackTrace.ToArray();

                Context.OnRenderException?.Invoke(this, ex);

                if (RethrowExceptions)
                    throw useEx;
                    
                var skipExecutingFilters = SkipExecutingFiltersIfError.GetValueOrDefault(Context.SkipExecutingFiltersIfError);
                if (skipExecutingFilters)
                    this.SkipFilterExecution = true;

                var rethrow = ScriptConfig.FatalExceptions.Contains(useEx.GetType());
                if (!rethrow)
                {
                    string errorBinding = null;

                    if (stopEx?.Options is Dictionary<string, object> filterParams)
                    {
                        if (filterParams.TryGetValue(ScriptConstants.AssignError, out object assignError))
                        {
                            errorBinding = assignError as string;
                        }
                        else if (filterParams.TryGetValue(ScriptConstants.CatchError, out object catchError))
                        {
                            errorBinding = catchError as string;
                            ResetError();
                        }
                        if (filterParams.TryGetValue(ScriptConstants.IfErrorReturn, out object ifErrorReturn))
                        {
                            ResetError();
                            return ifErrorReturn;
                        }
                    }

                    if (errorBinding == null)
                        errorBinding = AssignExceptionsTo ?? CatchExceptionsIn ?? Context.AssignExceptionsTo;

                    if (!string.IsNullOrEmpty(errorBinding))
                    {
                        if (CatchExceptionsIn != null)
                            ResetError();
                            
                        scope.ScopedParams[errorBinding] = useEx;
                        scope.ScopedParams[errorBinding + "StackTrace"] = stackTrace.Map(x => "   at " + x).Join(Environment.NewLine);
                        return string.Empty;
                    }
                }
                    
                //continueExecutingFiltersOnError == false / skipExecutingFiltersOnError == true 
                if (SkipExecutingFiltersIfError.HasValue || Context.SkipExecutingFiltersIfError)
                    return string.Empty;
                    
                // rethrow exceptions which aren't handled
                var exResult = Format.OnExpressionException(this, ex);
                if (exResult != null)
                    await scope.OutputStream.WriteAsync(Format.EncodeValue(exResult).ToUtf8Bytes(), token);
                else if (rethrow || useEx is TargetInvocationException)
                    throw useEx;

                var filterName = expr.GetDisplayName();
                if (filterName.StartsWith("throw"))
                    throw useEx;

                throw new TargetInvocationException($"Failed to invoke script method '{filterName}': {useEx.Message}", useEx);
            }
        }

        return UnwrapValue(value);
    }

    private void ResetError()
    {
        SkipFilterExecution = false;
        LastFilterError = null;
        LastFilterStackTrace = null;
    }

    private static object UnwrapValue(object value)
    {
        if (value == null || value == JsNull.Value || value == StopExecution.Value)
            return string.Empty; // treat as empty value if evaluated to null

        return value;
    }

    internal string CreateMissingFilterErrorMessage(string filterName)
    {
        var registeredMethods = ScriptMethods.Union(Context.ScriptMethods).ToList();
        var similarNonMatchingFilters = registeredMethods
            .SelectMany(x => x.QueryFilters(filterName))
            .Where(x => !(Context.ExcludeFiltersNamed.Contains(x.Name) || ExcludeFiltersNamed.Contains(x.Name)))
            .ToList();

        var sb = StringBuilderCache.Allocate()
            .AppendLine($"Filter in '{VirtualPath}' named '{filterName}' was not found.");

        if (similarNonMatchingFilters.Count > 0)
        {
            sb.Append("Check for correct usage in similar (but non-matching) filters:").AppendLine();
            var normalFilters = similarNonMatchingFilters
                .OrderBy(x => x.GetParameters().Length + (x.ReturnType == typeof(Task) ? 10 : 1))
                .ToArray();

            foreach (var mi in normalFilters)
            {
                var argsTypesWithoutContext = mi.GetParameters()
                    .Where(x => x.ParameterType != typeof(ScriptScopeContext))
                    .ToList();

                sb.Append("{{ ");

                if (argsTypesWithoutContext.Count == 0)
                {
                    sb.Append($"{mi.Name} => {mi.ReturnType.Name}");
                }
                else
                {
                    sb.Append($"{argsTypesWithoutContext[0].ParameterType.Name} |> {mi.Name}(");
                    var piCount = 0;
                    foreach (var pi in argsTypesWithoutContext.Skip(1))
                    {
                        if (piCount++ > 0)
                            sb.Append(", ");

                        sb.Append(pi.ParameterType.Name);
                    }

                    var returnType = mi.ReturnType == typeof(Task)
                        ? "(Stream)"
                        : mi.ReturnType.Name;

                    sb.Append($") => {returnType}");
                }

                sb.AppendLine(" }}");
            }
        }
        else
        {
            var registeredFilterNames = registeredMethods.Map(x => $"'{x.GetType().Name}'").Join(", ");
            sb.Append($"No similar filters named '{filterName}' were found in registered filter(s): {registeredFilterNames}.");
        }

        return StringBuilderCache.ReturnAndFree(sb);
    }

    // Filters with no args can be used in-place of bindings
    private MethodInvoker GetFilterAsBinding(string name, out ScriptMethods filter) => GetFilterInvoker(name, 0, out filter);
    private MethodInvoker GetContextFilterAsBinding(string name, out ScriptMethods filter) => GetContextFilterInvoker(name, 1, out filter);

    internal object InvokeFilter(MethodInvoker invoker, ScriptMethods filter, object[] args, string binding)
    {
        if (invoker == null)
            throw new NotSupportedException(CreateMissingFilterErrorMessage(binding.LeftPart('(')));

        try
        {
            return invoker(filter, args);
        }
        catch (StopFilterExecutionException) { throw; }
        catch (Exception ex)
        {
            var exResult = Format.OnExpressionException(this, ex);
            if (exResult != null)
                return exResult;

            if (binding.StartsWith("throw"))
                throw;

            throw new TargetInvocationException($"Failed to invoke script method '{binding}': {ex.Message}", ex);
        }
    }

    public ReadOnlySpan<char> ParseJsExpression(ScriptScopeContext scope, ReadOnlySpan<char> literal, out JsToken token)
    {
        try
        {
            return literal.ParseJsExpression(out token);
        }
        catch (ArgumentException e)
        {
            if (scope.ScopedParams.TryGetValue(nameof(PageVariableFragment), out var oVar)
                && oVar is PageVariableFragment var && !var.OriginalText.IsNullOrEmpty())
            {
                throw new Exception($"Invalid literal: {literal.ToString()} in '{var.OriginalText}'", e);
            }
                
            throw;
        }
    }

    private readonly Dictionary<string, ScriptBlock> blocksMap = new Dictionary<string, ScriptBlock>();

    public ScriptBlock TryGetBlock(string name) => blocksMap.TryGetValue(name, out var block) ? block : Context.GetBlock(name); 
    public ScriptBlock GetBlock(string name)
    {
        var block = TryGetBlock(name);
        if (block == null)
            throw new NotSupportedException($"Block in '{VirtualPath}' named '{name}' was not found.");

        return block;
    }       

    public ScriptScopeContext CreateScope(Stream outputStream=null) => 
        new ScriptScopeContext(this, outputStream ?? MemoryStreamFactory.GetStream(), null);

    internal MethodInvoker GetFilterInvoker(string name, int argsCount, out ScriptMethods filter) => GetInvoker(name, argsCount, InvokerType.Filter, out filter);
    internal MethodInvoker GetContextFilterInvoker(string name, int argsCount, out ScriptMethods filter) => GetInvoker(name, argsCount, InvokerType.ContextFilter, out filter);
    internal MethodInvoker GetContextBlockInvoker(string name, int argsCount, out ScriptMethods filter) => GetInvoker(name, argsCount, InvokerType.ContextBlock, out filter);

    private MethodInvoker GetInvoker(string name, int argsCount, InvokerType invokerType, out ScriptMethods filter)
    {
        if (!Context.ExcludeFiltersNamed.Contains(name) && !ExcludeFiltersNamed.Contains(name))
        {
            foreach (var tplFilter in ScriptMethods)
            {
                var invoker = tplFilter?.GetInvoker(name, argsCount, invokerType);
                if (invoker != null)
                {
                    filter = tplFilter;
                    return invoker;
                }
            }

            foreach (var tplFilter in Context.ScriptMethods)
            {
                var invoker = tplFilter?.GetInvoker(name, argsCount, invokerType);
                if (invoker != null)
                {
                    filter = tplFilter;
                    return invoker;
                }
            }
        }

        filter = null;
        return null;
    }

    public object EvaluateIfToken(object value, ScriptScopeContext scope)
    {
        if (value is JsToken token)
            return token.Evaluate(scope);

        return value;
    }

    internal bool TryGetValue(string name, ScriptScopeContext scope, bool argsOnly, out object value)
    {
        if (name == null)
            throw new ArgumentNullException(nameof(name));

        MethodInvoker invoker;
        var ret = true;

        value = scope.ScopedParams != null && scope.ScopedParams.TryGetValue(name, out object obj)
            ? obj
            : name == ScriptConstants.Global
                ? Args
                : Args.TryGetValue(name, out obj)
                    ? obj
                    : Page != null && Page.Args.TryGetValue(name, out obj)
                        ? obj
                        : CodePage != null && CodePage.Args.TryGetValue(name, out obj)
                            ? obj
                            : LayoutPage != null && LayoutPage.Args.TryGetValue(name, out obj)
                                ? obj
                                : Context.Args.TryGetValue(name, out obj)
                                    ? obj
                                    : argsOnly 
                                        ? null
                                        : (invoker = GetFilterAsBinding(name, out var filter)) != null
                                            ? InvokeFilter(invoker, filter, new object[0], name)
                                            : (invoker = GetContextFilterAsBinding(name, out filter)) != null
                                                ? InvokeFilter(invoker, filter, new object[] {scope}, name)
                                                // ReSharper disable once ExpressionIsAlwaysNull
                                                // ReSharper disable once ConditionalTernaryEqualBranch
#pragma warning disable CS0665
                                                : ((ret = false) ? null : null);
#pragma warning restore CS0665
        return ret;
    }
        
    internal object GetValue(string name, ScriptScopeContext scope)
    {
        TryGetValue(name, scope, argsOnly:false, out var value);
        return value;
    }

    internal object GetArgument(string name, ScriptScopeContext scope)
    {
        TryGetValue(name, scope, argsOnly:true, out var value);
        return value;
    }

    public string ResultOutput => resultOutput;

    private string resultOutput;
    public string Result
    {
        get
        {
            try
            {
                if (resultOutput != null)
                    return resultOutput;
    
                Init().Wait();
                resultOutput = this.RenderToStringAsync().Result;
                return resultOutput;
            }
            catch (AggregateException e)
            {
                var ex = e.UnwrapIfSingleException();
                throw ex;
            }
        }
    }

    public PageResult Execute()
    {
        var render = Result;
        return this;
    }

    public PageResult Clone(SharpPage page)
    {
        return new PageResult(page)
        {
            Args = Args,
            ScriptMethods = ScriptMethods,
            ScriptBlocks = ScriptBlocks,
            FilterTransformers = FilterTransformers,
        };
    }

    public void Dispose()
    {
        CodePage?.Dispose();
    }
}

public class BindingExpressionException : Exception
{
    public string Expression { get; }
    public string Member { get; }

    public BindingExpressionException(string message, string member, string expression, Exception inner=null)
        : base(message, inner)
    {
        Expression = expression;
        Member = member;
    }
}

public class SyntaxErrorException : ArgumentException
{
    public SyntaxErrorException() { }
    public SyntaxErrorException(string message) : base(message) { }
    public SyntaxErrorException(string message, Exception innerException) : base(message, innerException) { }
}