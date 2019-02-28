using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

        public static implicit operator Templates.TemplateScopeContext(ScriptScopeContext from)
        {
            return new Templates.TemplateScopeContext(from.PageResult, from.OutputStream, from.ScopedParams);
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
        public static object GetValue(this ScriptScopeContext scope, string name)
        {
            return scope.PageResult.GetValue(name, scope);
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
            await scope.PageResult.WritePageAsync(page, codePage, scope.ScopeWithParams(pageParams), token);
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