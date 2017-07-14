using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Web;

#if NETSTANDARD1_3
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public interface IPageResult {}

    // Render a Template Page to the Response OutputStream
    public class PageResult : IPageResult, IStreamWriterAsync, IHasOptions
    {
        public TemplatePage Page { get; set; }

        public TemplatePage LayoutPage { get; set; }

        public object Model { get; set; }

        /// <summary>
        /// Add additional Args available to all pages 
        /// </summary>
        public Dictionary<string, object> Args { get; set; }

        /// <summary>
        /// Add additional template filters available to all pages
        /// </summary>
        public List<TemplateFilter> TemplateFilters { get; set; }

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

        public PageResult(TemplatePage page)
        {
            Page = page ?? throw new ArgumentNullException(nameof(page));
            Args = new Dictionary<string, object>();
            TemplateFilters = new List<TemplateFilter>();
            PageTransformers = new List<Func<Stream, Task<Stream>>>();
            OutputTransformers = new List<Func<Stream, Task<Stream>>>();
            FilterTransformers = new Dictionary<string, Func<Stream, Task<Stream>>>();
            Options = new Dictionary<string, string>
            {
                {HttpHeaders.ContentType, Page.Format.ContentType},
            };
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
            }
        }

        internal async Task WriteToAsyncInternal(Stream outputStream, CancellationToken token)
        {
            await Init();

            if (LayoutPage != null)
            {
                await Task.WhenAll(LayoutPage.Init(), Page.Init());
            }
            else
            {
                await Page.Init();
                if (Page.LayoutPage != null)
                {
                    LayoutPage = Page.LayoutPage;
                    await LayoutPage.Init();
                }
            }

            token.ThrowIfCancellationRequested();

            var pageScopeContext = CreatePageContext(null, outputStream);

            if (LayoutPage != null)
            {
                foreach (var fragment in LayoutPage.PageFragments)
                {
                    if (fragment is PageStringFragment str)
                    {
                        await outputStream.WriteAsync(str.ValueBytes, token);
                    }
                    else if (fragment is PageVariableFragment var)
                    {
                        if (var.Binding.Equals(TemplateConstants.Page))
                        {
                            await WritePageAsync(Page, pageScopeContext, token);
                        }
                        else
                        {
                            await WriteVarAsync(pageScopeContext, var, token);
                        }
                    }
                }
            }
            else
            {
                await WritePageAsync(Page, pageScopeContext, token);
            }
        }

        private bool hasInit;
        public async Task<PageResult> Init()
        {
            if (hasInit)
                return this;

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
                Page.Context.InitFilter(filter);
            }

            await Page.Init();

            hasInit = true;

            return this;
        }

        private void AssertInit()
        {
            if (!hasInit)
                throw new NotSupportedException("PageResult.Init() required for this operation.");
        }

        public async Task WritePageAsync(TemplatePage page, TemplateScopeContext scopeContext, CancellationToken token = default(CancellationToken))
        {
            if (PageTransformers.Count == 0)
            {
                await WritePageAsyncInternal(page, scopeContext, token);
                return;
            }

            //If PageResult has any PageFilters Buffer and chain stream responses to each
            using (var ms = MemoryStreamFactory.GetStream())
            {
                await WritePageAsyncInternal(page, new TemplateScopeContext(this, ms, scopeContext.ScopedParams), token);
                Stream stream = ms;

                foreach (var transformer in PageTransformers)
                {
                    stream.Position = 0;
                    stream = await transformer(stream);
                }

                using (stream)
                {
                    stream.Position = 0;
                    await stream.CopyToAsync(scopeContext.OutputStream);
                }
            }
        }

        internal async Task WritePageAsyncInternal(TemplatePage page, TemplateScopeContext scopeContext, CancellationToken token = default(CancellationToken))
        {
            if (!page.HasInit)
                await page.Init();
            
            foreach (var fragment in page.PageFragments)
            {
                if (fragment is PageStringFragment str)
                {
                    await scopeContext.OutputStream.WriteAsync(str.ValueBytes, token);
                }
                else if (fragment is PageVariableFragment var)
                {
                    await WriteVarAsync(scopeContext, var, token);
                }
            }
        }

        private async Task WriteVarAsync(TemplateScopeContext scopeContext, PageVariableFragment var, CancellationToken token)
        {
            MethodInvoker blockFilterInvoker = null;
            TemplateFilter filter = null;
            var expr = var.FilterExpressions != null && var.FilterExpressions.Length > 0 ? var.FilterExpressions[0] : null;
            if (expr != null)
            {
                foreach (var tplFilter in TemplateFilters)
                {
                    blockFilterInvoker = tplFilter.GetContextInvoker(expr.Name, expr.Args.Count + 2);
                    if (blockFilterInvoker != null)
                    {
                        filter = tplFilter;
                        break;
                    }
                }
                foreach (var tplFilter in Page.Context.TemplateFilters)
                {
                    blockFilterInvoker = tplFilter.GetContextInvoker(expr.Name, expr.Args.Count + 2);
                    if (blockFilterInvoker != null)
                    {
                        filter = tplFilter;
                        break;
                    }
                }
            }

            if (blockFilterInvoker != null)
            {
                var hasFilterTransformers = var.FilterExpressions.Length > 1;
                
                var args = new object[2 + expr.Args.Count];
                var useScope = hasFilterTransformers 
                    ? scopeContext.ScopeWithStream(MemoryStreamFactory.GetStream()) 
                    : scopeContext;

                args[0] = useScope;
                args[1] = GetValue(var, scopeContext);  // filter target

                for (var cmdIndex = 0; cmdIndex < expr.Args.Count; cmdIndex++)
                {
                    var arg = expr.Args[cmdIndex];
                    var varValue = Evaluate(var, arg, scopeContext);
                    varValue = EvaluateAnyBindings(varValue, scopeContext);
                    args[2 + cmdIndex] = varValue;
                }

                try
                {
                    await (Task) blockFilterInvoker(filter, args);

                    if (hasFilterTransformers)
                    {
                        using (useScope.OutputStream)
                        {
                            var stream = useScope.OutputStream;

                            //If Context Filter has any Filter Transformers Buffer and chain stream responses to each
                            for (var i = 1; i < var.FilterExpressions.Length; i++)
                            {
                                var transformer = GetFilterTransformer(var.FilterExpressions[i].Name.Value);
                                if (transformer == null)
                                    throw new NotSupportedException($"Could not find FilterTransformer '{expr.Name}'");
                                
                                stream.Position = 0;
                                stream = await transformer(stream);
                            }
                            
                            stream.Position = 0;
                            await stream.CopyToAsync(scopeContext.OutputStream);
                        }
                    }
                }
                catch (Exception ex)
                {
                    var exResult = Page.Format.OnExpressionException(this, ex);
                    if (exResult != null)
                    {
                        await scopeContext.OutputStream.WriteAsync(Page.Format.EncodeValue(exResult).ToUtf8Bytes(), token);
                    }

                    throw new TargetInvocationException($"Failed to invoke filter {expr.Binding}", ex);
                }
            }
            else
            {
                var value = Evaluate(var, scopeContext);
                var bytes = value != null
                    ? Page.Format.EncodeValue(value).ToUtf8Bytes()
                    : var.OriginalTextBytes;

                await scopeContext.OutputStream.WriteAsync(bytes, token);
            }
        }

        public Func<Stream, Task<Stream>> GetFilterTransformer(string name)
        {
            return FilterTransformers.TryGetValue(name, out Func<Stream, Task<Stream>> fn)
                ? fn
                : Page.Context.FilterTransformers.TryGetValue(name, out fn)
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

        private object GetValue(PageVariableFragment var, TemplateScopeContext scopeContext)
        {
            var value = var.Value ??
                (var.Binding.HasValue ? GetValue(var.NameString, scopeContext) : null);
            
            return value;
        }

        private object Evaluate(PageVariableFragment var, TemplateScopeContext scopeContext)
        {
            var value = var.Value ??
                (var.Binding.HasValue
                    ? GetValue(var.NameString, scopeContext)
                    : var.Expression != null
                        ? var.Expression.IsBinding()
                            ? EvaluateBinding(var.Expression.Name, scopeContext)
                            : Evaluate(var, var.Expression, scopeContext)
                        : null);

            if (value == null)
            {
                if (!var.Binding.HasValue) 
                    return null;

                var hasFilterAsBinding = GetFilterAsBinding(var.Binding, out TemplateFilter filter);
                if (hasFilterAsBinding != null)
                {
                    value = InvokeFilter(hasFilterAsBinding, filter, new object[0], var.Expression?.Binding ?? var.Binding);
                }
                else
                {
                    var handlesUnknownValue = false;
                    if (var.FilterExpressions.Length > 0)
                    {
                        var filterName = var.FilterExpressions[0].Name;
                        var filterArgs = 1 + var.FilterExpressions[0].Args.Count;
                        handlesUnknownValue = TemplateFilters.Any(x => x.HandlesUnknownValue(filterName, filterArgs)) ||
                                              Page.Context.TemplateFilters.Any(x => x.HandlesUnknownValue(filterName, filterArgs));
                    }

                    if (!handlesUnknownValue)
                        return null;
                }
            }

            if (value == JsNull.Value)
                value = null;

            value = EvaluateAnyBindings(value, scopeContext);

            for (var i = 0; i < var.FilterExpressions.Length; i++)
            {
                var expr = var.FilterExpressions[i];
                var invoker = GetFilterInvoker(expr.Name, 1 + expr.Args.Count, out TemplateFilter filter);
                if (invoker == null)
                {
                    if (i == 0)
                        return null; // ignore on server if first filter is missing  

                    throw new Exception($"Filter not found: {expr} in '{Page.VirtualPath}'");
                }

                var args = new object[1 + expr.Args.Count];
                args[0] = value;

                for (var cmdIndex = 0; cmdIndex < expr.Args.Count; cmdIndex++)
                {
                    var arg = expr.Args[cmdIndex];
                    var varValue = Evaluate(var, arg, scopeContext);
                    args[1 + cmdIndex] = varValue;
                }

                value = InvokeFilter(invoker, filter, args, expr.Binding);
            }

            if (value == null)
                return string.Empty; // treat as empty value if evaluated to null

            return value;
        }

        // Filters with no args can be used in-place of bindings
        private MethodInvoker GetFilterAsBinding(StringSegment name, out TemplateFilter filter) => GetFilterInvoker(name, 0, out filter);

        private object EvaluateAnyBindings(object value, TemplateScopeContext scopeContext)
        {
            if (value is JsBinding valueBinding)
            {
                return GetValue(valueBinding.Binding.Value, scopeContext);
            }
            if (value is Dictionary<string, object> map)
            {
                var keys = map.Keys.ToArray();
                foreach (var key in keys)
                {
                    var entryValue = map[key];
                    map[key] = EvaluateAnyBindings(entryValue, scopeContext);
                }
            }
            if (value is List<object> list)
            {
                for (var i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    list[i] = EvaluateAnyBindings(item, scopeContext);
                }
            }
            return value;
        }

        private object InvokeFilter(MethodInvoker invoker, TemplateFilter filter, object[] args, StringSegment binding)
        {
            if (invoker == null)
                throw new NotSupportedException($"Filter {binding} does not exist");

            try
            {
                return invoker(filter, args);
            }
            catch (Exception ex)
            {
                var exResult = Page.Format.OnExpressionException(this, ex);
                if (exResult != null)
                    return exResult;
                
                throw new TargetInvocationException($"Failed to invoke filter {binding}", ex);
            }
        }

        private object Evaluate(PageVariableFragment var, StringSegment arg, TemplateScopeContext scopeContext)
        {
            var.ParseLiteral(arg, out object outValue, out JsBinding binding);

            if (binding is JsExpression expr)
            {
                var value = Evaluate(var, expr, scopeContext);
                return value;
            }
            if (binding != null)
            {
                return GetValue(binding.Binding.Value, scopeContext);
            }
            return outValue;
        }

        private object Evaluate(PageVariableFragment var, JsExpression expr, TemplateScopeContext scopeContext)
        {
            var invoker = GetFilterInvoker(expr.Name, expr.Args.Count, out TemplateFilter filter);

            var args = new object[expr.Args.Count];
            for (var i = 0; i < expr.Args.Count; i++)
            {
                var arg = expr.Args[i];
                var varValue = Evaluate(var, arg, scopeContext);
                args[i] = varValue;
            }

            var value = InvokeFilter(invoker, filter, args, expr.Binding);
            return value;
        }

        private MethodInvoker GetFilterInvoker(StringSegment name, int argsCount, out TemplateFilter filter)
        {
            foreach (var tplFilter in TemplateFilters)
            {
                var invoker = tplFilter.GetInvoker(name, argsCount);
                if (invoker != null)
                {
                    filter = tplFilter;
                    return invoker;
                }
            }

            foreach (var tplFilter in Page.Context.TemplateFilters)
            {
                var invoker = tplFilter.GetInvoker(name, argsCount);
                if (invoker != null)
                {
                    filter = tplFilter;
                    return invoker;
                }
            }

            filter = null;
            return null;
        }

        private object GetValue(string name, TemplateScopeContext scopedParams)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            MethodInvoker invoker;

            var value = scopedParams.ScopedParams != null && scopedParams.ScopedParams.TryGetValue(name, out object obj)
                ? obj
                : Args.TryGetValue(name, out obj)
                    ? obj
                    : Page.Args.TryGetValue(name, out obj)
                        ? obj
                        : (LayoutPage != null && LayoutPage.Args.TryGetValue(name, out obj))
                            ? obj
                            : Page.Context.Args.TryGetValue(name, out obj)
                                ? obj
                                : (invoker = GetFilterAsBinding(name.ToStringSegment(), out TemplateFilter filter)) != null
                                    ? InvokeFilter(invoker, filter, new object[0], name.ToStringSegment())
                                    : null;

            if (value is JsBinding binding)
            {
                return GetValue(binding.Binding.Value, scopedParams);
            }
            
            return value;
        }

        private static readonly char[] VarDelimiters = { '.', '[', ' ' };

        public object EvaluateBinding(string expr, TemplateScopeContext scopeContext = default(TemplateScopeContext))
        {
            return EvaluateBinding(expr.ToStringSegment(), scopeContext);
        }

        public object EvaluateBinding(StringSegment expr, TemplateScopeContext scopeContext)
        {
            if (expr.IsNullOrWhiteSpace())
                return null;
            
            AssertInit();
            
            expr = expr.Trim();
            var pos = expr.IndexOfAny(VarDelimiters, 0);
            if (pos == -1)
                return GetValue(expr.Value, scopeContext);
            
            var target = expr.Substring(0, pos);

            var targetValue = GetValue(target, scopeContext);
            if (targetValue == null)
                return null;

            if (targetValue == JsNull.Value)
                return JsNull.Value;

            var fn = !Page.IsTempFile
                ? Page.Context.GetExpressionBinder(targetValue.GetType(), expr)
                : TemplatePageUtils.Compile(targetValue.GetType(), expr);

            try
            {
                var value = fn(targetValue);
                return value;
            }
            catch (Exception ex)
            {
                var exResult = Page.Format.OnExpressionException(this, ex);
                if (exResult != null)
                    return exResult;
                
                throw new BindingExpressionException($"Could not evaluate expression '{expr}'", null, expr.Value, ex);
            }
        }

        private string result;
        public string Result
        {
            get
            {
                try
                {
                    if (result != null)
                        return result;
    
                    Init().Wait();
                    result = this.RenderToStringAsync().Result;
                    return result;
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