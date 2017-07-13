using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        public Dictionary<string, object> ScopedParams { get; internal set; }
        public Stream OutputStream { get; }

        public TemplateScopeContext(PageResult pageResult, Stream outputStream, Dictionary<string, object> scopedParams)
        {
            PageResult = pageResult;
            ScopedParams = scopedParams;
            OutputStream = outputStream;
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
                     x.GetParameters().Length == argsCount + 1 && 
                     x.GetParameters()[0].ParameterType == typeof(TemplateScopeContext) && x.ReturnType == typeof(Task<object>));
            return method;
        }
    }
}