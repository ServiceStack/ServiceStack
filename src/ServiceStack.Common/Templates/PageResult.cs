using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Web;

#if NETSTANDARD2_0
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public interface IPageResult {}

    // Render a Template Page to the Response OutputStream
    public class PageResult : IPageResult, IStreamWriterAsync, IHasOptions, IDisposable
    {
        public TemplatePage Page { get; }
        
        public TemplateCodePage CodePage { get; }

        /// <summary>
        /// Use specified Layout 
        /// </summary>
        public TemplatePage LayoutPage { get; set; }
        
        /// <summary>
        /// Use Layout with specified name
        /// </summary>
        public string Layout { get; set; }
        
        /// <summary>
        /// Render without any Layout
        /// </summary>
        public bool NoLayout { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public object Model { get; set; }

        /// <summary>
        /// Add additional Args available to all pages 
        /// </summary>
        public Dictionary<string, object> Args { get; set; }

        /// <summary>
        /// Add additional template filters available to all pages
        /// </summary>
        public List<TemplateFilter> TemplateFilters { get; set; }

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
        /// Whether to skip execution of all page filters and just write template string fragments
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
        
        private readonly Stack<string> stackTrace = new Stack<string>();

        private PageResult(PageFormat format)
        {
            Args = new Dictionary<string, object>();
            TemplateFilters = new List<TemplateFilter>();
            PageTransformers = new List<Func<Stream, Task<Stream>>>();
            OutputTransformers = new List<Func<Stream, Task<Stream>>>();
            FilterTransformers = new Dictionary<string, Func<Stream, Task<Stream>>>();
            Options = new Dictionary<string, string>
            {
                {HttpHeaders.ContentType, format?.ContentType},
            };
        }

        public PageResult(TemplatePage page) : this(page?.Format)
        {
            Page = page ?? throw new ArgumentNullException(nameof(page));
        }

        public PageResult(TemplateCodePage page) : this(page?.Format)
        {
            CodePage = page ?? throw new ArgumentNullException(nameof(page));

            var hasRequest = (CodePage as IRequiresRequest)?.Request;
            if (hasRequest != null)
                Args[TemplateConstants.Request] = hasRequest;
        }

        public async Task WriteToAsync(Stream responseStream, CancellationToken token = default(CancellationToken))
        {
            if (OutputTransformers.Count == 0)
            {
                await WriteToAsyncInternal(responseStream, token);
                return;
            }

            //If PageResult has any OutputFilters Buffer and chain stream responses to each
            using (var ms = MemoryStreamFactory.GetStream())
            {
                stackTrace.Push("OutputTransformer");
                
                await WriteToAsyncInternal(ms, token);
                Stream stream = ms;

                foreach (var transformer in OutputTransformers)
                {
                    stream.Position = 0;
                    stream = await transformer(stream);
                }

                using (stream)
                {
                    stream.Position = 0;
                    await stream.CopyToAsync(responseStream);
                }

                stackTrace.Pop();
            }
        }

        internal async Task WriteToAsyncInternal(Stream outputStream, CancellationToken token)
        {
            await Init();

            if (!NoLayout)
            {
                if (LayoutPage != null)
                {
                    await LayoutPage.Init();
                
                    if (CodePage != null)
                        InitIfNewPage(CodePage);

                    if (Page != null)
                        await InitIfNewPage(Page);
                }
                else
                {
                    if (Page != null)
                    {
                        await InitIfNewPage(Page);
                        if (Page.LayoutPage != null)
                        {
                            LayoutPage = Page.LayoutPage;
                            await LayoutPage.Init();
                        }
                    }
                    else if (CodePage != null)
                    {
                        InitIfNewPage(CodePage);
                        if (CodePage.LayoutPage != null)
                        {
                            LayoutPage = CodePage.LayoutPage;
                            await LayoutPage.Init();
                        }
                    }
                }
            }
            else
            {
                if (Page != null)
                {
                    await InitIfNewPage(Page);
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
                stackTrace.Push("Layout: " + LayoutPage.VirtualPath);
                
                foreach (var fragment in LayoutPage.PageFragments)
                {
                    if (HaltExecution)
                        break;

                    if (fragment is PageStringFragment str)
                    {
                        await outputStream.WriteAsync(str.ValueBytes, token);
                    }
                    else if (fragment is PageVariableFragment var && !ShouldSkipFilterExecution(var))
                    {
                        if (var.Binding.Equals(TemplateConstants.Page))
                        {
                            await WritePageAsync(Page, CodePage, pageScope, token);
                        }
                        else
                        {
                            await WriteVarAsync(pageScope, var, token);
                        }
                    }
                }

                stackTrace.Pop();
            }
            else
            {
                await WritePageAsync(Page, CodePage, pageScope, token);
            }
        }

        public bool ShouldSkipFilterExecution(PageVariableFragment var)
        {
            return HaltExecution || SkipFilterExecution && (var.BindingString != null 
               ? !TemplateConfig.OnlyEvaluateFiltersWhenSkippingPageFilterExecution.Contains(var.BindingString)
               : var.InitialExpression?.NameString == null || 
                 !TemplateConfig.OnlyEvaluateFiltersWhenSkippingPageFilterExecution.Contains(var.InitialExpression.NameString));
        }

        public TemplateContext Context => Page?.Context ?? CodePage.Context;
        public PageFormat Format => Page?.Format ?? CodePage.Format;
        public string VirtualPath => Page?.VirtualPath ?? CodePage.VirtualPath;

        private bool hasInit;
        public async Task<PageResult> Init()
        {
            if (hasInit)
                return this;

            if (!Context.HasInit)
                throw new NotSupportedException($"{Context.GetType().Name} has not been initialized. Call 'Init()' to initialize Template Context.");

            if (Model != null)
            {
                var explodeModel = Model.ToObjectDictionary();
                foreach (var entry in explodeModel)
                {
                    Args[entry.Key] = entry.Value ?? JsNull.Value;
                }
            }
            Args[TemplateConstants.Model] = Model ?? JsNull.Value;

            foreach (var filter in TemplateFilters)
            {
                Context.InitFilter(filter);
            }

            if (Page != null)
            {
                await Page.Init();
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

        private Task InitIfNewPage(TemplatePage page) => page != Page 
            ? (Task) page.Init() 
            : TypeConstants.EmptyTask;

        private void InitIfNewPage(TemplateCodePage page)
        {
            if (page != CodePage)
                page.Init();
        }

        private void AssertInit()
        {
            if (!hasInit)
                throw new NotSupportedException("PageResult.Init() required for this operation.");
        }

        public Task WritePageAsync(TemplatePage page, TemplateCodePage codePage,
            TemplateScopeContext scope, CancellationToken token = default(CancellationToken))
        {
            if (page != null)
                return WritePageAsync(page, scope, token);

            return WriteCodePageAsync(codePage, scope, token);
        }

        public async Task WritePageAsync(TemplatePage page, TemplateScopeContext scope, CancellationToken token = default(CancellationToken))
        {
            if (PageTransformers.Count == 0)
            {
                await WritePageAsyncInternal(page, scope, token);
                return;
            }

            //If PageResult has any PageFilters Buffer and chain stream responses to each
            using (var ms = MemoryStreamFactory.GetStream())
            {
                stackTrace.Push("PageTransformer");

                await WritePageAsyncInternal(page, new TemplateScopeContext(this, ms, scope.ScopedParams), token);
                Stream stream = ms;

                foreach (var transformer in PageTransformers)
                {
                    stream.Position = 0;
                    stream = await transformer(stream);
                }

                using (stream)
                {
                    stream.Position = 0;
                    await stream.CopyToAsync(scope.OutputStream);
                }

                stackTrace.Pop();
            }
        }

        internal async Task WritePageAsyncInternal(TemplatePage page, TemplateScopeContext scope, CancellationToken token = default(CancellationToken))
        {
            await page.Init(); //reload modified changes if needed

            stackTrace.Push("Page: " + page.VirtualPath);
            
            foreach (var fragment in page.PageFragments)
            {
                if (HaltExecution)
                    break;

                if (fragment is PageStringFragment str)
                {
                    await scope.OutputStream.WriteAsync(str.ValueBytes, token);
                }
                else if (fragment is PageVariableFragment var && !ShouldSkipFilterExecution(var))
                {
                    await WriteVarAsync(scope, var, token);
                }
            }
            
            stackTrace.Pop();
        }

        public async Task WriteCodePageAsync(TemplateCodePage page, TemplateScopeContext scope, CancellationToken token = default(CancellationToken))
        {
            if (PageTransformers.Count == 0)
            {
                await WriteCodePageAsyncInternal(page, scope, token);
                return;
            }

            //If PageResult has any PageFilters Buffer and chain stream responses to each
            using (var ms = MemoryStreamFactory.GetStream())
            {
                await WriteCodePageAsyncInternal(page, new TemplateScopeContext(this, ms, scope.ScopedParams), token);
                Stream stream = ms;

                foreach (var transformer in PageTransformers)
                {
                    stream.Position = 0;
                    stream = await transformer(stream);
                }

                using (stream)
                {
                    stream.Position = 0;
                    await stream.CopyToAsync(scope.OutputStream);
                }
            }
        }

        internal Task WriteCodePageAsyncInternal(TemplateCodePage page, TemplateScopeContext scope, CancellationToken token = default(CancellationToken))
        {
            page.Scope = scope;

            if (!page.HasInit)
                page.Init();

            return page.WriteAsync(scope);
        }

        private string toDebugString(object instance)
        {
            using (JsConfig.With(excludeTypeInfo:true, includeTypeInfo:false))
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

        private async Task WriteVarAsync(TemplateScopeContext scope, PageVariableFragment var, CancellationToken token)
        {
            if (var.BindingString != null)
                stackTrace.Push("Expression (binding): " + var.BindingString);
            else if (var.InitialExpression?.NameString != null)
                stackTrace.Push("Expression (filter): " + var.InitialExpression.NameString);
            else if (var.InitialValue != null)
                stackTrace.Push($"Expression ({var.InitialValue.GetType().Name}): " + toDebugString(var.InitialValue).SubstringWithElipsis(0, 200));
            else 
                stackTrace.Push("Expression");
            
            var value = await EvaluateAsync(var, scope, token);
            if (value != IgnoreResult.Value)
            {
                if (value != null)
                {
                    var bytes = Format.EncodeValue(value).ToUtf8Bytes();
                    await scope.OutputStream.WriteAsync(bytes, token);
                }
                else
                {
                    var bytes = Context.OnUnhandledExpression(var);
                    if (bytes != null && bytes.Length > 0)
                        await scope.OutputStream.WriteAsync(bytes, token);
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
                if (var.FilterExpressions[0].Args.Count > 0)
                {
                    var.FilterExpressions[0].Args[0].ParseNextToken(out object argValue, out _);
                    scopedParams = argValue as Dictionary<string, object>;
                }
            }
            return scopedParams;
        }

        private TemplateScopeContext CreatePageContext(PageVariableFragment var, Stream outputStream) => new TemplateScopeContext(this, outputStream, GetPageParams(var));

        private async Task<object> EvaluateAsync(PageVariableFragment var, TemplateScopeContext scope, CancellationToken token=default(CancellationToken))
        {
            var value = var.InitialValue ??
                (var.Binding.HasValue
                    ? GetValue(var.BindingString, scope)
                    : var.InitialExpression != null
                        ? EvaluateExpression(var.InitialExpression, scope, var)
                        : null);

            if (value == null)
            {
                var handlesUnknownValue = HandlesUnknownValue(var);
                if (!handlesUnknownValue)
                {
                    if (var.InitialExpression != null && var.InitialExpression.IsBinding)
                    {
                        var expr = var.InitialExpression.NameString;
                        expr = expr.Trim();
                        var pos = expr.IndexOfAny(VarDelimiters, 0);
                        if (pos > 0)
                        {
                            var target = expr.Substring(0, pos);

                            //allow nested null bindings from an existing target to evaluate to an empty string 
                            var targetValue = GetValue(target, scope);
                            if (targetValue != null)
                                return string.Empty;
                        }
                    }

                    if (!var.Binding.HasValue)
                        return null;

                    var hasFilterAsBinding = GetFilterAsBinding(var.BindingString, out TemplateFilter filter);
                    if (hasFilterAsBinding != null)
                    {
                        value = InvokeFilter(hasFilterAsBinding, filter, new object[0], var.BindingString);
                    }
                    else
                    {
                        var hasContexFilterAsBinding = GetContextFilterAsBinding(var.BindingString, out filter);
                        if (hasContexFilterAsBinding != null)
                        {
                            value = InvokeFilter(hasContexFilterAsBinding, filter, new object[] { scope }, var.BindingString);
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

            value = EvaluateAnyBindings(value, scope);
            
            for (var i = 0; i < var.FilterExpressions.Length; i++)
            {
                if (HaltExecution || value == StopExecution.Value)
                    break;

                var expr = var.FilterExpressions[i];

                try
                {
                    var filterName = expr.NameString;
                    var invoker = GetFilterInvoker(filterName, 1 + expr.Args.Count, out TemplateFilter filter);
                    var contextFilterInvoker = invoker == null
                        ? GetContextFilterInvoker(filterName, 2 + expr.Args.Count, out filter)
                        : null;
                    var contextBlockInvoker = invoker == null && contextFilterInvoker == null
                        ? GetContextBlockInvoker(filterName, 2 + expr.Args.Count, out filter)
                        : null;

                    if (invoker == null && contextFilterInvoker == null && contextBlockInvoker == null)
                    {
                        if (i == 0)
                            return null; // ignore on server (i.e. assume it's on client) if first filter is missing  

                        var errorMsg = CreateMissingFilterErrorMessage(filterName);
                        throw new Exception(errorMsg);
                    }

                    if (invoker != null)
                    {
                        var args = new object[1 + expr.Args.Count];
                        args[0] = value;

                        for (var cmdIndex = 0; cmdIndex < expr.Args.Count; cmdIndex++)
                        {
                            var arg = expr.Args[cmdIndex];
                            var varValue = EvaluateAnyBindings(EvaluateBindingExpression(arg, scope, var), scope);
                            args[1 + cmdIndex] = varValue;
                        }

                        value = InvokeFilter(invoker, filter, args, expr.BindingString);
                    }
                    else if (contextFilterInvoker != null)
                    {
                        var args = new object[2 + expr.Args.Count];

                        args[0] = scope;
                        args[1] = value;  // filter target

                        for (var cmdIndex = 0; cmdIndex < expr.Args.Count; cmdIndex++)
                        {
                            var arg = expr.Args[cmdIndex];
                            var varValue = EvaluateAnyBindings(EvaluateBindingExpression(arg, scope, var), scope);
                            args[2 + cmdIndex] = varValue;
                        }

                        value = InvokeFilter(contextFilterInvoker, filter, args, expr.BindingString);
                    }
                    else
                    {
                        var hasFilterTransformers = var.FilterExpressions.Length + i > 1;

                        var args = new object[2 + expr.Args.Count];
                        var useScope = hasFilterTransformers
                            ? scope.ScopeWithStream(MemoryStreamFactory.GetStream())
                            : scope;

                        args[0] = useScope;
                        args[1] = value;  // filter target

                        for (var cmdIndex = 0; cmdIndex < expr.Args.Count; cmdIndex++)
                        {
                            var arg = expr.Args[cmdIndex];
                            var varValue = EvaluateAnyBindings(EvaluateBindingExpression(arg, scope, var), scope);
                            args[2 + cmdIndex] = varValue;
                        }

                        try
                        {
                            var taskResponse = (Task)contextBlockInvoker(filter, args);
                            await taskResponse;

                            if (hasFilterTransformers)
                            {
                                using (useScope.OutputStream)
                                {
                                    var stream = useScope.OutputStream;

                                    //If Context Filter has any Filter Transformers Buffer and chain stream responses to each
                                    for (var exprIndex = i + 1; exprIndex < var.FilterExpressions.Length; exprIndex++)
                                    {
                                        stream.Position = 0;

                                        contextBlockInvoker = GetContextBlockInvoker(var.FilterExpressions[exprIndex].NameString, 1 + var.FilterExpressions[exprIndex].Args.Count, out filter);
                                        if (contextBlockInvoker != null)
                                        {
                                            args[0] = useScope;
                                            for (var cmdIndex = 0; cmdIndex < var.FilterExpressions[exprIndex].Args.Count; cmdIndex++)
                                            {
                                                var arg = var.FilterExpressions[exprIndex].Args[cmdIndex];
                                                var varValue = EvaluateAnyBindings(EvaluateBindingExpression(arg, scope, var), scope);
                                                args[1 + cmdIndex] = varValue;
                                            }

                                            await (Task)contextBlockInvoker(filter, args);
                                        }
                                        else
                                        {
                                            var transformer = GetFilterTransformer(var.FilterExpressions[exprIndex].NameString);
                                            if (transformer == null)
                                                throw new NotSupportedException($"Could not find FilterTransformer '{var.FilterExpressions[exprIndex].NameString}' in page '{Page.VirtualPath}'");

                                            stream = await transformer(stream);
                                            useScope = useScope.ScopeWithStream(stream);
                                        }
                                    }

                                    if (stream.CanRead)
                                    {
                                        stream.Position = 0;
                                        await stream.CopyToAsync(scope.OutputStream);
                                    }
                                }
                            }
                        }
                        catch (StopFilterExecutionException) { throw; }
                        catch (Exception ex)
                        {
                            var rethrow = TemplateConfig.FatalExceptions.Contains(ex.GetType());

                            var exResult = Format.OnExpressionException(this, ex);
                            if (exResult != null)
                                await scope.OutputStream.WriteAsync(Format.EncodeValue(exResult).ToUtf8Bytes(), token);
                            else if (rethrow)
                                throw;

                            throw new TargetInvocationException($"Failed to invoke filter: {expr.GetDisplayName()}", ex);
                        }

                        return IgnoreResult.Value;
                    }

                    if (value is Task<object> valueTask)
                        value = await valueTask;
                }
                catch (StopFilterExecutionException ex)
                {
                    LastFilterError = ex.InnerException;
                    LastFilterStackTrace = stackTrace.ToArray();

                    if (RethrowExceptions)
                        throw ex.InnerException;
                    
                    var skipExecutingFilters = SkipExecutingFiltersIfError.GetValueOrDefault(Context.SkipExecutingFiltersIfError);
                    if (skipExecutingFilters)
                        this.SkipFilterExecution = true;

                    var rethrow = TemplateConfig.FatalExceptions.Contains(ex.InnerException.GetType());
                    if (!rethrow)
                    {
                        string errorBinding = null;

                        if (ex.Options is Dictionary<string, object> filterParams && filterParams.TryGetValue(TemplateConstants.AssignError, out object assignError))
                            errorBinding = assignError as string;

                        if (errorBinding == null)
                            errorBinding = AssignExceptionsTo ?? Context.AssignExceptionsTo;

                        if (!string.IsNullOrEmpty(errorBinding))
                        {
                            scope.ScopedParams[errorBinding] = ex.InnerException;
                            return string.Empty;
                        }
                    }
                    
                    if (SkipExecutingFiltersIfError.HasValue || Context.SkipExecutingFiltersIfError)
                        return string.Empty;
                    
                    // rethrow exceptiosn which aren't handled

                    var exResult = Format.OnExpressionException(this, ex);
                    if (exResult != null)
                        await scope.OutputStream.WriteAsync(Format.EncodeValue(exResult).ToUtf8Bytes(), token);
                    else if (rethrow)
                        throw ex.InnerException;

                    var filterName = expr.GetDisplayName();
                    if (filterName.StartsWith("throw"))
                        throw ex.InnerException;

                    throw new TargetInvocationException($"Failed to invoke filter: {filterName}", ex.InnerException);
                }
            }

            if (value == null || value == JsNull.Value || value == StopExecution.Value)
                return string.Empty; // treat as empty value if evaluated to null

            return value;
        }

        private bool HandlesUnknownValue(PageVariableFragment var)
        {
            if (var.FilterExpressions.Length > 0)
            {
                var filterName = var.FilterExpressions[0].NameString;
                var filterArgs = 1 + var.FilterExpressions[0].Args.Count;
                return TemplateFilters.Any(x => x.HandlesUnknownValue(filterName, filterArgs)) 
                    || Context.TemplateFilters.Any(x => x.HandlesUnknownValue(filterName, filterArgs));
            }
            return false;
        }

        private string CreateMissingFilterErrorMessage(string filterName)
        {
            var registeredFilters = TemplateFilters.Union(Context.TemplateFilters).ToList();
            var similarNonMatchingFilters = registeredFilters
                .SelectMany(x => x.QueryFilters(filterName))
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
                        .Where(x => x.ParameterType != typeof(TemplateScopeContext))
                        .ToList();

                    sb.Append("{{ ");

                    if (argsTypesWithoutContext.Count == 0)
                    {
                        sb.Append($"{mi.Name} => {mi.ReturnType.Name}");
                    }
                    else
                    {
                        sb.Append($"{argsTypesWithoutContext[0].ParameterType.Name} | {mi.Name}(");
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
                var registeredFilterNames = registeredFilters.Map(x => $"'{x.GetType().Name}'").Join(", ");
                sb.Append($"No similar filters named '{filterName}' were found in registered filter(s): {registeredFilterNames}.");
            }

            return StringBuilderCache.ReturnAndFree(sb);
        }

        // Filters with no args can be used in-place of bindings
        private MethodInvoker GetFilterAsBinding(string name, out TemplateFilter filter) => GetFilterInvoker(name, 0, out filter);
        private MethodInvoker GetContextFilterAsBinding(string name, out TemplateFilter filter) => GetContextFilterInvoker(name, 1, out filter);

        public object EvaluateAnyBindings(object value, TemplateScopeContext scope)
        {
            if (value is JsExpression expr)
                return EvaluateExpression(expr, scope);
            
            if (value is JsBinding valueBinding)
                return GetValue(valueBinding.BindingString, scope);
            
            if (value is JsConstant constant)
                return constant.Value;

            if (value is Dictionary<string, object> map)
            {
                var clone = new Dictionary<string, object>();
                var keys = map.Keys.ToArray();
                foreach (var key in keys)
                {
                    var entryValue = map[key];
                    clone[key] = EvaluateAnyBindings(entryValue, scope);
                }
                return clone;
            }
            if (value is List<object> list)
            {
                var clone = new List<object>();
                foreach (var item in list)
                {
                    clone.Add(EvaluateAnyBindings(item, scope));
                }
                return clone;
            }
            return value;
        }

        private object InvokeFilter(MethodInvoker invoker, TemplateFilter filter, object[] args, string binding)
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

                throw new TargetInvocationException($"Failed to invoke filter {binding}", ex);
            }
        }

        private object EvaluateExpression(JsExpression expr, TemplateScopeContext scope, PageVariableFragment var = null)
        {
            var value = expr.IsBinding
                ? EvaluateBinding(expr.NameString, scope)
                : expr.Args.Count > 0 
                    ? EvaluateMethod(expr, scope, var) 
                    : EvaluateBindingExpression(expr.Binding, scope, var);
            return value;
        }

        private object EvaluateBindingExpression(StringSegment arg, TemplateScopeContext scope, PageVariableFragment var=null)
        {
            object outValue;
            JsBinding binding;
            
            if (var == null)
                arg = arg.ParseNextToken(out outValue, out binding);
            else
                arg = var.ParseNextToken(arg, out outValue, out binding);

            var unaryOp = JsUnaryOperator.GetUnaryOperator(binding);
            if (unaryOp != null)
            {
                arg = var == null 
                    ? arg.ParseNextToken(out outValue, out binding) 
                    : var.ParseNextToken(arg, out outValue, out binding);
            }
            
            object value = null;
            if (binding is JsExpression expr)
            {
                value = EvaluateToken(scope, expr);
            }
            else
            {
                value = binding != null 
                    ? GetValue(binding.BindingString, scope) 
                    : EvaluateAnyBindings(outValue, scope);
            }

            if (unaryOp != null)
                value = unaryOp.Evaluate(value);
            
            return value;
        }

        private object EvaluateMethod(JsExpression expr, TemplateScopeContext scope, PageVariableFragment var=null)
        {
            if (expr.Name.IsNullOrEmpty())
                throw new ArgumentNullException("expr.Name");
            
            var invoker = GetFilterInvoker(expr.NameString, expr.Args.Count, out TemplateFilter filter);
            if (invoker != null)
            {
                var args = new object[expr.Args.Count];
                for (var i = 0; i < expr.Args.Count; i++)
                {
                    var arg = expr.Args[i];
                    var varValue = EvaluateBindingExpression(arg, scope, var);
                    args[i] = varValue;
                }

                var value = InvokeFilter(invoker, filter, args, expr.BindingString);
                return value;
            }

            invoker = GetContextFilterInvoker(expr.NameString, expr.Args.Count + 1, out filter);
            if (invoker != null)
            {
                var args = new object[expr.Args.Count + 1];
                args[0] = scope;
                for (var i = 0; i < expr.Args.Count; i++)
                {
                    var arg = expr.Args[i];
                    var varValue = EvaluateBindingExpression(arg, scope, var);
                    args[i + 1] = varValue;
                }

                var value = InvokeFilter(invoker, filter, args, expr.BindingString);
                return value;
            }

            throw new NotSupportedException(CreateMissingFilterErrorMessage(expr.BindingString.LeftPart('(')));
        }

        private MethodInvoker GetFilterInvoker(string name, int argsCount, out TemplateFilter filter) => GetInvoker(name, argsCount, InvokerType.Filter, out filter);
        private MethodInvoker GetContextFilterInvoker(string name, int argsCount, out TemplateFilter filter) => GetInvoker(name, argsCount, InvokerType.ContextFilter, out filter);
        private MethodInvoker GetContextBlockInvoker(string name, int argsCount, out TemplateFilter filter) => GetInvoker(name, argsCount, InvokerType.ContextBlock, out filter);

        private MethodInvoker GetInvoker(string name, int argsCount, InvokerType invokerType, out TemplateFilter filter)
        {
            if (!Context.ExcludeFiltersNamed.Contains(name) && !ExcludeFiltersNamed.Contains(name))
            {
                foreach (var tplFilter in TemplateFilters)
                {
                    var invoker = tplFilter?.GetInvoker(name, argsCount, invokerType);
                    if (invoker != null)
                    {
                        filter = tplFilter;
                        return invoker;
                    }
                }

                foreach (var tplFilter in Context.TemplateFilters)
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

        public object EvaluateToken(TemplateScopeContext scope, JsToken token)
        {
            if (token is UnaryExpression u)
            {
                token = new JsConstant(u.Evaluate(scope));
            }
            
            return token is JsExpression expr && expr.Args.Count > 0
                ? EvaluateMethod(expr, scope)
                : EvaluateAnyBindings(token, scope);
        }

        internal object GetValue(string name, TemplateScopeContext scope)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            MethodInvoker invoker;

            var value = scope.ScopedParams != null && scope.ScopedParams.TryGetValue(name, out object obj)
                ? obj
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
                                    : (invoker = GetFilterAsBinding(name, out TemplateFilter filter)) != null
                                        ? InvokeFilter(invoker, filter, new object[0], name)
                                        : (invoker = GetContextFilterAsBinding(name, out filter)) != null
                                             ? InvokeFilter(invoker, filter, new object[]{ scope }, name)
                                             : null;

            if (value is JsBinding binding)
            {
                return GetValue(binding.BindingString, scope);
            }
            
            return value;
        }

        private static readonly char[] VarDelimiters = { '.', '[', ' ' };

        public object EvaluateBinding(string expr, TemplateScopeContext scope = default(TemplateScopeContext))
        {
            if (string.IsNullOrWhiteSpace(expr))
                return null;
            
            AssertInit();
            
            expr = expr.Trim();
            var pos = expr.IndexOfAny(VarDelimiters, 0);
            if (pos == -1)
                return GetValue(expr, scope);
            
            var target = expr.Substring(0, pos);

            var targetValue = GetValue(target, scope);
            if (targetValue == null)
                return null;

            if (targetValue == JsNull.Value)
                return JsNull.Value;
            
            var fn = Context.GetExpressionBinder(targetValue.GetType(), expr.ToStringSegment());

            try
            {
                var value = fn(scope, targetValue);
                return value;
            }
            catch (KeyNotFoundException)
            {
                return JsNull.Value;
            }
            catch (Exception ex)
            {
                var exResult = Format.OnExpressionException(this, ex);
                if (exResult != null)
                    return exResult;
                
                throw new BindingExpressionException($"Could not evaluate expression '{expr}'", null, expr, ex);
            }
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
                    throw e.UnwrapIfSingleException();
                }
            }
        }

        public PageResult Clone(TemplatePage page)
        {
            return new PageResult(page)
            {
                Args = Args,
                TemplateFilters = TemplateFilters,
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
}