using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

#if NETSTANDARD2_0
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public struct TemplateScopeContext
    {
        public PageResult PageResult { get; }
        public TemplatePage Page => PageResult.Page;
        public TemplateCodePage CodePage => PageResult.CodePage;
        public TemplateContext Context => PageResult.Context;
        public Dictionary<string, object> ScopedParams { get; internal set; }
        public Stream OutputStream { get; }

        public TemplateScopeContext(PageResult pageResult, Stream outputStream, Dictionary<string, object> scopedParams)
        {
            PageResult = pageResult;
            ScopedParams = scopedParams ?? new Dictionary<string, object>();
            OutputStream = outputStream;
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

    public static class TemplateScopeContextUtils
    {
        public static object GetValue(this TemplateScopeContext scope, string name)
        {
            return scope.PageResult.GetValue(name, scope);
        }
        
        public static TemplateScopeContext CreateScopedContext(this TemplateScopeContext scope, string template, Dictionary<string, object> scopeParams = null, bool cachePage=true)
        {
            TemplatePage dynamicPage = null;

            if (cachePage)
            {
                scope.Context.Cache.TryGetValue(template, out object value);
                dynamicPage = value as TemplatePage;
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
            var itemScope = new TemplateScopeContext(pageResult, scope.OutputStream, newScopeParams);

            return itemScope;
        }

        public static Task WritePageAsync(this TemplateScopeContext scope) => scope.PageResult.WritePageAsync(scope.Page, scope);

        public static TemplateScopeContext ScopeWithParams(this TemplateScopeContext parentContext, Dictionary<string, object> scopedParams)
        {
            if (scopedParams == null)
                return parentContext;

            if (parentContext.ScopedParams.Count == 0)
                return new TemplateScopeContext(parentContext.PageResult, parentContext.OutputStream, scopedParams);
            
            var to = new Dictionary<string, object>();
            foreach (var entry in parentContext.ScopedParams)
            {
                to[entry.Key] = entry.Value;
            }
            foreach (var entry in scopedParams)
            {
                to[entry.Key] = entry.Value;
            }
            return new TemplateScopeContext(parentContext.PageResult, parentContext.OutputStream, to);
        }
        
        public static TemplateScopeContext ScopeWithStream(this TemplateScopeContext scope, Stream stream) =>
            new TemplateScopeContext(scope.PageResult, stream, scope.ScopedParams);

        public static async Task WritePageAsync(this TemplateScopeContext scope, TemplatePage page, TemplateCodePage codePage, Dictionary<string, object> pageParams, CancellationToken token = default(CancellationToken))
        {
            await scope.PageResult.WritePageAsync(page, codePage, scope.ScopeWithParams(pageParams), token);
        }

        public static object EvaluateToken(this TemplateScopeContext scope, JsToken token)
        {
            var result = scope.PageResult.EvaluateToken(scope, token);
            return result;
        }

        public static object Evaluate(this TemplateScopeContext scope, object value, JsToken token)
        {
            var result = token != null
                ? scope.PageResult.EvaluateToken(scope, token)
                : scope.PageResult.EvaluateAnyBindings(value, scope);
            return result;
        }

        public static void InvokeAssignExpression(this TemplateScopeContext scope, string assignExpr, object target, object value)
        {
            var fn = scope.Context.GetAssignExpression(target.GetType(), assignExpr.ToStringSegment());

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