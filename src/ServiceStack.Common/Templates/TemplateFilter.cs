using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServiceStack.Text;

#if NETSTANDARD1_3
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class HandleUnknownValueAttribute : AttributeBase {}
    
    public class TemplateFilter
    {
        public TemplatePagesContext Context { get; set; }
        public ITemplatePages Pages { get; set; }
        
        readonly ConcurrentDictionary<string, MethodInvoker> invokerCache = new ConcurrentDictionary<string, MethodInvoker>();

        public virtual bool HandlesUnknownValue(StringSegment name, int argsCount)
        {
            var method = GetInvokerMethod(name, argsCount);
            return method != null && method.FirstAttribute<HandleUnknownValueAttribute>() != null;
        }

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
            var method = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public)
                .FirstOrDefault(x => name.EqualsIgnoreCase(x.Name) && x.GetParameters().Length == argsCount);
            return method;
        }
    }
}