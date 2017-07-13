using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Text;

#if NETSTANDARD1_3
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class HandleUnknownValueAttribute : AttributeBase {}

    public struct TemplateScopeContext
    {
        public PageResult PageResult { get; }
        public TemplatePage Page => PageResult.Page;
        public TemplatePagesContext Context => Page.Context;
        public Dictionary<string, object> ScopedParams { get; internal set; }
        public Stream OutputStream { get; }

        public TemplateScopeContext(PageResult pageResult, Stream outputStream, Dictionary<string, object> scopedParams)
        {
            PageResult = pageResult;
            ScopedParams = scopedParams;
            OutputStream = outputStream;
        }
    }

    public static class TemplateScopeContextUtils
    {
        public static TemplateScopeContext CreateScopedContext(this TemplateScopeContext scope, string template)
        {
            var dynamicPage = scope.Context.OneTimePage(template);
            scope.Page.Args.Each((x,y) => dynamicPage.Args[x] = y);
            var pageResult = new PageResult(dynamicPage) {
                Args = scope.PageResult.Args
            }.Init().Result;

            var itemScope = new TemplateScopeContext(pageResult, scope.OutputStream, 
                scope.ScopedParams == null ? new Dictionary<string, object>() : new Dictionary<string, object>(scope.ScopedParams));

            return itemScope;
        }

        public static async Task WritePageAsync(this TemplateScopeContext scope)
        {
            await scope.PageResult.WritePageAsync(scope.Page, scope);            
        }

        public static TemplateScopeContext ScopeWithParams(this TemplateScopeContext parentContext, Dictionary<string, object> scopedParams)
        {
            if (scopedParams == null)
                return parentContext;

            if (parentContext.ScopedParams == null)
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

        public static async Task WritePageAsync(this TemplateScopeContext scope, TemplatePage page, Dictionary<string, object> pageParams, CancellationToken token = default(CancellationToken))
        {
            await scope.PageResult.WritePageAsync(page, scope.ScopeWithParams(pageParams), token);
        }
    }
    
    public class TemplateFilter
    {
        public TemplatePagesContext Context { get; set; }
        public ITemplatePages Pages { get; set; }
        
        public virtual bool HandlesUnknownValue(StringSegment name, int argsCount)
        {
            var method = GetInvokerMethod(name, argsCount);
            return method?.AllAttributes().Any(x => x is HandleUnknownValueAttribute) == true;
        }

        readonly ConcurrentDictionary<string, MethodInvoker> invokerCache = new ConcurrentDictionary<string, MethodInvoker>();

        public MethodInvoker GetInvoker(StringSegment name, int argsCount)
        {
            var key = $"{name}`{argsCount}";

            if (invokerCache.TryGetValue(key, out MethodInvoker invoker))
                return invoker;

            var method = GetInvokerMethod(name, argsCount);
            if (method == null)
                return null;

            invoker = TypeExtensions.GetInvokerToCache(method);

            invokerCache[key] = invoker;
            
            return invoker;
        }

        private MethodInfo GetInvokerMethod(StringSegment name, int argsCount)
        {
            var method = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(x => name.EqualsIgnoreCase(x.Name) && 
                     x.GetParameters().Length == argsCount && 
                     (argsCount == 0 || x.GetParameters()[0].ParameterType != typeof(TemplateScopeContext)));
            return method;
        }

        public MethodInvoker GetContextInvoker(StringSegment name, int argsCount)
        {
            var key = $"{name}`{argsCount}";

            if (invokerCache.TryGetValue(key, out MethodInvoker invoker))
                return invoker;

            var method = GetContextInvokerMethod(name, argsCount);
            if (method == null)
                return null;

            invoker = TypeExtensions.GetInvokerToCache(method);

            invokerCache[key] = invoker;
            
            return invoker;
        }

        private MethodInfo GetContextInvokerMethod(StringSegment name, int argsCount)
        {
            var method = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(x => name.EqualsIgnoreCase(x.Name) && 
                     x.GetParameters().Length == argsCount && 
                     x.GetParameters()[0].ParameterType == typeof(TemplateScopeContext));
            
            if (method != null && method.ReturnType != typeof(Task))
                throw new NotSupportedException($"Filter '{name}' with scope context does not have a Task return type");
            
            return method;
        }
    }
}