using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    public struct ScriptScopeContext
    {
        public PageResult PageResult { get; }
        public SharpPage Page => PageResult.Page;
        public SharpCodePage CodePage => PageResult.CodePage;
        public ScriptContext Context => PageResult.Context;
        public Dictionary<string, object> ScopedParams { get; internal set; }
        public Stream OutputStream { get; }

        public ScriptScopeContext(PageResult pageResult, Stream outputStream, Dictionary<string, object> scopedParams)
        {
            PageResult = pageResult;
            ScopedParams = scopedParams ?? new Dictionary<string, object>();
            OutputStream = outputStream;
        }

        public ScriptScopeContext(ScriptContext context, Dictionary<string, object> scopedParams)
        {
            PageResult = new PageResult(context.EmptyPage);
            OutputStream = null;
            ScopedParams = scopedParams;
        }

        public ScriptScopeContext Clone()
        {
            return new ScriptScopeContext(PageResult, OutputStream, new Dictionary<string, object>(ScopedParams));
        }
    }

    public class ScopeVars : Dictionary<string, object>
    {
        public ScopeVars() {}
        public ScopeVars(IDictionary<string, object> dictionary) : base(dictionary) {}
        public ScopeVars(IDictionary<string, object> dictionary, IEqualityComparer<string> comparer) : base(dictionary, comparer) {}
        public ScopeVars(IEqualityComparer<string> comparer) : base(comparer) {}
        public ScopeVars(int capacity) : base(capacity) {}
        public ScopeVars(int capacity, IEqualityComparer<string> comparer) : base(capacity, comparer) {}
    }

    public static class ScriptScopeContextUtils
    {
        public static StopExecution ReturnValue(this ScriptScopeContext scope, object returnValue, Dictionary<string, object> returnArgs=null)
        {
            scope.PageResult.ReturnValue = new ReturnValue(returnValue, returnArgs); 
            scope.PageResult.HaltExecution = true;
            return StopExecution.Value;
        }

        /// <summary>
        /// Resolve value from stored arguments and filters 
        /// </summary>
        public static object GetValue(this ScriptScopeContext scope, string name) => scope.PageResult.GetValue(name, scope);
        
        /// <summary>
        /// Resolve value from stored arguments only 
        /// </summary>
        public static object GetArgument(this ScriptScopeContext scope, string name) => scope.PageResult.GetArgument(name, scope);
        
        /// <summary>
        /// Try Resolve value from stored arguments and filters 
        /// </summary>
        public static bool TryGetValue(this ScriptScopeContext scope, string name, out object value) => 
            scope.PageResult.TryGetValue(name, scope, argsOnly:false, out value);

        public static bool TryGetMethod(this ScriptScopeContext scope, string name, int fnArgValuesCount, out Delegate fn, out ScriptMethods scriptMethod, out bool requiresScope)
        {
            scriptMethod = null;
            requiresScope = false;
            var result = scope.PageResult;
            
            fn = scope.GetValue(name) as Delegate;
            if (fn == null)
                fn = result.GetFilterInvoker(name, fnArgValuesCount, out scriptMethod);
            
            if (fn == null)
            {
                fn = result.GetContextFilterInvoker(name, fnArgValuesCount + 1, out scriptMethod);
                if (fn == null)
                {
                    var contextFilter = result.GetContextBlockInvoker(name, fnArgValuesCount + 1, out scriptMethod);
                    if (contextFilter != null)
                    {
                        // Other languages require captured output of Context Blocks
                        var filter = scriptMethod;
                        fn = (StaticMethodInvoker) (args => {
                            var ctxScope = (ScriptScopeContext) args[0];
                            using var ms = MemoryStreamFactory.GetStream();
                            args[0] = ctxScope.ScopeWithStream(ms);
                            var task = (Task) contextFilter(filter, args);
                            task.Wait();
                            var discard = task.GetResult();

                            var ret = MemoryProvider.Instance.FromUtf8(ms.GetBufferAsMemory().Span);
                            return ret.ToString();
                        });
                    }
                }
                if (fn != null)
                    requiresScope = true;
            }
            
            return fn != null;
        }

        public static object EvaluateExpression(this ScriptScopeContext scope, string expr) //used in test only
        {
            expr.ParseJsExpression(out var token);
            return token.Evaluate(scope);
        }
        
        public static ScriptScopeContext CreateScopedContext(this ScriptScopeContext scope, string template, Dictionary<string, object> scopeParams = null, bool cachePage=true)
        {
            SharpPage dynamicPage = null;

            if (cachePage)
            {
                scope.Context.Cache.TryGetValue(template, out object value);
                dynamicPage = value as SharpPage;
            }

            if (dynamicPage == null)
            {
                dynamicPage = scope.Context.OneTimePage(template);

                if (cachePage)
                {
                    scope.Context.Cache[template] = dynamicPage;
                }
            }

            var newScopeParams = new Dictionary<string, object>(scope.ScopedParams);
            scopeParams.Each((key,val) => newScopeParams[key] = val);

            var pageResult = scope.PageResult.Clone(dynamicPage).Init().Result;
            var itemScope = new ScriptScopeContext(pageResult, scope.OutputStream, newScopeParams);

            return itemScope;
        }

        public static Task WritePageAsync(this ScriptScopeContext scope) => scope.PageResult.WritePageAsync(scope.Page, scope);

        public static ScriptScopeContext ScopeWithParams(this ScriptScopeContext parentContext, Dictionary<string, object> scopedParams)
            => ScopeWith(parentContext, scopedParams, parentContext.OutputStream);
        
        public static ScriptScopeContext ScopeWith(this ScriptScopeContext parentContext, Dictionary<string, object> scopedParams=null, Stream outputStream=null)
        {
            if (scopedParams == null && outputStream == null)
                return parentContext;
            
            if (scopedParams == null)
                scopedParams = parentContext.ScopedParams;
            
            if (outputStream == null)
                outputStream = parentContext.OutputStream;

            if (parentContext.ScopedParams.Count == 0)
                return new ScriptScopeContext(parentContext.PageResult, outputStream, scopedParams);
            
            var to = new Dictionary<string, object>();
            foreach (var entry in parentContext.ScopedParams)
            {
                to[entry.Key] = entry.Value;
            }
            foreach (var entry in scopedParams)
            {
                to[entry.Key] = entry.Value;
            }
            return new ScriptScopeContext(parentContext.PageResult, outputStream, to);
        }
        
        public static ScriptScopeContext ScopeWithStream(this ScriptScopeContext scope, Stream stream) =>
            new ScriptScopeContext(scope.PageResult, stream, scope.ScopedParams);

        public static async Task WritePageAsync(this ScriptScopeContext scope, SharpPage page, SharpCodePage codePage, Dictionary<string, object> pageParams, CancellationToken token = default(CancellationToken))
        {
            await scope.PageResult.WritePageAsync(page, codePage, scope.ScopeWithParams(pageParams), token).ConfigAwait();
        }

        public static void InvokeAssignExpression(this ScriptScopeContext scope, string assignExpr, object target, object value)
        {
            var fn = scope.Context.GetAssignExpression(target.GetType(), assignExpr.AsMemory());

            try
            {
                fn(scope, target, value);
            }
            catch (Exception ex)
            {
                throw new BindingExpressionException($"Could not evaluate assign expression '{assignExpr}'", null, assignExpr, ex);
            }
        }
    }
}