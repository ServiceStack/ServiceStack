using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace ServiceStack.Templates
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class HandleUnknownValueAttribute : AttributeBase {}

    public enum InvokerType
    {
        Filter,
        ContextFilter,
        ContextBlock,
    }
    
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

        public MethodInvoker GetInvoker(string name, int argsCount, InvokerType type) => type == InvokerType.Filter
            ? GetInvoker(name, argsCount)
            : type == InvokerType.ContextFilter
                ? GetContextFilterInvoker(name, argsCount)
                : GetContextBlockInvoker(name, argsCount);

        // Normal Filters
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

        // Filters which require access to the TemplateScopeContext but act like a normal filter
        public MethodInvoker GetContextFilterInvoker(string name, int argsCount)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            
            if (Context.ExcludeFiltersNamed.Contains(name))
                return null;
            
            var key = $"context-filter::{name}`{argsCount}";
            if (invokerCache.TryGetValue(key, out MethodInvoker invoker))
                return invoker;

            var method = GetContextFilterInvokerMethod(name, argsCount);
            if (method == null)
                return null;

            return invokerCache[key] = TypeExtensions.GetInvokerToCache(method);
        }

        private MethodInfo GetContextFilterInvokerMethod(string name, int argsCount)
        {
            var method = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(x => name.EqualsIgnoreCase(x.Name) && 
                     x.GetParameters().Length == argsCount && 
                     x.GetParameters()[0].ParameterType == typeof(TemplateScopeContext) &&
                     x.ReturnType != typeof(Task)); //Returns results like normal filters, i.e. don't write to the OutputStream 
            
            return method;
        }

        // Filters which write directly to the OutputStream
        public MethodInvoker GetContextBlockInvoker(string name, int argsCount)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            
            if (Context.ExcludeFiltersNamed.Contains(name))
                return null;
            
            var key = $"context-block::{name}`{argsCount}";
            if (invokerCache.TryGetValue(key, out MethodInvoker invoker))
                return invoker;

            var method = GetContextBlockInvokerMethod(name, argsCount);
            if (method == null)
                return null;

            return invokerCache[key] = TypeExtensions.GetInvokerToCache(method);
        }

        private MethodInfo GetContextBlockInvokerMethod(string name, int argsCount)
        {
            var method = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(x => name.EqualsIgnoreCase(x.Name) && 
                     x.GetParameters().Length == argsCount && 
                     x.GetParameters()[0].ParameterType == typeof(TemplateScopeContext) &&
                     x.ReturnType == typeof(Task)); //Context Block Filters require Task return Type as they should write to the Output Stream
            
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

        public static IEnumerable AssertEnumerable(this object items, string filterName)
        {
            var enumItems = items as IEnumerable;
            if (enumItems == null && items != null)
                throw new ArgumentException(
                    $"{filterName} expects an IEnumerable but received a '{items.GetType().Name}' instead");

            return enumItems ?? TypeConstants.EmptyObjectArray;
        }
    }
}