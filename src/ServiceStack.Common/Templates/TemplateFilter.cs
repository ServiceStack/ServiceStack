using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ServiceStack.Templates
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class HandleUnknownValueAttribute : AttributeBase {}
    
    public class TemplateFilter
    {
        public TemplatePagesContext Context { get; set; }
        public ITemplatePages Pages { get; set; }
        
        public virtual bool HandlesUnknownValue(string name, int argsCount)
        {
            var method = GetInvokerMethod(name, argsCount);
            return method?.AllAttributes().Any(x => x is HandleUnknownValueAttribute) == true;
        }

        readonly ConcurrentDictionary<string, MethodInvoker> invokerCache = new ConcurrentDictionary<string, MethodInvoker>();

        public MethodInvoker GetInvoker(string name, int argsCount)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            
            if (Context.ExcludeFiltersNamed.Contains(name))
                return null;
            
            var key = $"{name}`{argsCount}";
            if (invokerCache.TryGetValue(key, out MethodInvoker invoker))
                return invoker;

            var method = GetInvokerMethod(name, argsCount);
            if (method == null)
                return null;

            return invokerCache[key] = TypeExtensions.GetInvokerToCache(method);
        }

        private MethodInfo GetInvokerMethod(string name, int argsCount)
        {
            var method = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(x => name.EqualsIgnoreCase(x.Name) && 
                     x.GetParameters().Length == argsCount && 
                     (argsCount == 0 || x.GetParameters()[0].ParameterType != typeof(TemplateScopeContext)));
            return method;
        }

        public MethodInvoker GetContextInvoker(string name, int argsCount)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            
            if (Context.ExcludeFiltersNamed.Contains(name))
                return null;
            
            var key = $"context::{name}`{argsCount}";
            if (invokerCache.TryGetValue(key, out MethodInvoker invoker))
                return invoker;

            var method = GetContextInvokerMethod(name, argsCount);
            if (method == null)
                return null;

            return invokerCache[key] = TypeExtensions.GetInvokerToCache(method);
        }

        private MethodInfo GetContextInvokerMethod(string name, int argsCount)
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

    public static class TemplateFilterUtils
    {
        public static Dictionary<string, object> AssertOptions(this TemplateScopeContext scope, string filterName, object scopedParams)
        {
            var pageParams = scopedParams as Dictionary<string, object>;
            if (pageParams == null && scopedParams != null)
                throw new ArgumentException(
                    $"{filterName} in '{scope.Page.File.VirtualPath}' only accepts an Object dictionary as an argument but received a '{scopedParams.GetType().Name}' instead");

            return pageParams ?? new Dictionary<string, object>();
        }
        
        public static Dictionary<string, object> AssertOptions(this object scopedParams, string filterName)
        {
            var pageParams = scopedParams as Dictionary<string, object>;
            if (pageParams == null && scopedParams != null)
                throw new ArgumentException(
                    $"{filterName} only accepts an Object dictionary as an argument but received a '{scopedParams.GetType().Name}' instead");

            return pageParams ?? new Dictionary<string, object>();
        }
    }
}