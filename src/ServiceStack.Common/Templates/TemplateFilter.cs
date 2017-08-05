using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Web;

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

    public interface IResultInstruction {}
    public class IgnoreResult : IResultInstruction
    {
        internal static readonly IgnoreResult Value = new IgnoreResult();
        private IgnoreResult(){}
    }

    public class TemplateFilter
    {
        public TemplateContext Context { get; set; }
        public ITemplatePages Pages { get; set; }
        
        public virtual bool HandlesUnknownValue(string name, int argsCount)
        {
            var method = GetInvokerMethod(name, argsCount);
            return method?.AllAttributes().Any(x => x is HandleUnknownValueAttribute) == true;
        }

        public List<MethodInfo> QueryFilters(string filterName)
        {
            var filters = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.Name.IndexOf(filterName, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            return filters;
        }

        public ConcurrentDictionary<string, MethodInvoker> InvokerCache { get; } = new ConcurrentDictionary<string, MethodInvoker>();

        public MethodInvoker GetInvoker(string name, int argsCount, InvokerType type) => type == InvokerType.Filter
            ? GetInvoker(name, argsCount)
            : type == InvokerType.ContextFilter
                ? GetContextFilterInvoker(name, argsCount)
                : GetContextBlockInvoker(name, argsCount);
        
        // Normal Filters
        private MethodInvoker GetInvoker(string name, int argsCount)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var key = name + "`" + argsCount;
            if (InvokerCache.TryGetValue(key, out MethodInvoker invoker))
                return invoker;

            var method = GetInvokerMethod(name, argsCount);
            if (method == null)
                return null;

            return InvokerCache[key] = TypeExtensions.GetInvokerToCache(method);
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
        private MethodInvoker GetContextFilterInvoker(string name, int argsCount)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            
            var key = "context-filter::" + name + "`" + argsCount;
            if (InvokerCache.TryGetValue(key, out MethodInvoker invoker))
                return invoker;

            var method = GetContextFilterInvokerMethod(name, argsCount);
            if (method == null)
                return null;

            return InvokerCache[key] = TypeExtensions.GetInvokerToCache(method);
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
        private MethodInvoker GetContextBlockInvoker(string name, int argsCount)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            
            var key = "context-block::" + name + "`" + argsCount;
            if (InvokerCache.TryGetValue(key, out MethodInvoker invoker))
                return invoker;

            var method = GetContextBlockInvokerMethod(name, argsCount);
            if (method == null)
                return null;

            return InvokerCache[key] = TypeExtensions.GetInvokerToCache(method);
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

        public static object AssertNoCircularDeps(this object value)
        {
            if (value != null && TypeSerializer.HasCircularReferences(value))
                throw new NotSupportedException($"Cannot serialize type '{value.GetType().Name}' with cyclical dependencies");
            return value;
        }

        public static IEnumerable<object> AssertEnumerable(this object items, string filterName)
        {
            var enumObjects = items as IEnumerable<object>;
            if (enumObjects == null && items != null)
            {
                if (items is IEnumerable enumItems)
                {
                    var to = new List<object>();
                    foreach (var item in enumItems)
                    {
                        to.Add(item);
                    }
                    return to;
                }
                
                throw new ArgumentException(
                    $"{filterName} expects an IEnumerable but received a '{items.GetType().Name}' instead");
            }

            return enumObjects ?? TypeConstants.EmptyObjectArray;
        }

        public static string AssertExpression(this TemplateScopeContext scope, string filterName, object expression)
        {
            if (!(expression is string literal)) 
                throw new NotSupportedException($"'{nameof(filterName)}' in '{scope.Page.VirtualPath}' requires a string Expression but received a '{expression?.GetType()?.Name}' instead");
            return literal;
        }

        public static Dictionary<string, object> GetParamsWithItemBinding(this TemplateScopeContext scope, string filterName, object scopedParams, out string itemBinding) =>
            GetParamsWithItemBinding(scope, filterName, null, scopedParams, out itemBinding);

        public static Dictionary<string, object> GetParamsWithItemBinding(this TemplateScopeContext scope, string filterName, TemplatePage page, object scopedParams, out string itemBinding)
        {
            var scopeParams = scope.GetParamsWithItemBindingOnly(filterName, page, scopedParams, out itemBinding);
            scopeParams.Each((key, val) => scope.ScopedParams[key] = val);
            return scopeParams;
        }

        public static Dictionary<string, object> GetParamsWithItemBindingOnly(this TemplateScopeContext scope, string filterName, TemplatePage page, object scopedParams, out string itemBinding)
        {
            var pageParams = scope.AssertOptions(filterName, scopedParams);
            itemBinding = pageParams.TryGetValue("it", out object bindingName) && bindingName is string binding
                ? binding
                : "it";
            
            if (bindingName != null && !(bindingName is string))
                throw new NotSupportedException($"'it' option in filter '{filterName}' should contain the name to bind to but contained a '{bindingName.GetType().Name}' instead");

            // page vars take precedence
            if (page != null && page.Args.TryGetValue("it", out object pageBinding))
                itemBinding = (string)pageBinding;
            
            return pageParams;
        }

        public static TemplateScopeContext AddItemToScope(this TemplateScopeContext scope, string itemBinding, object item, int index)
        {
            scope.ScopedParams[TemplateConstants.Index] = index;
            return scope.AddItemToScope(itemBinding, item);
        }

        public static TemplateScopeContext AddItemToScope(this TemplateScopeContext scope, string itemBinding, object item)
        {
            scope.ScopedParams[itemBinding] = item;

            var explodeBindings = item as ScopeVars;
            if (explodeBindings != null)
            {
                foreach (var entry in explodeBindings)
                {
                    scope.ScopedParams[entry.Key] = entry.Value;
                }
            }
            return scope;
        }

        public static T GetValueOrEvaluateBinding<T>(this TemplateScopeContext scope, object valueOrBinding) =>
            (T)GetValueOrEvaluateBinding(scope, valueOrBinding, typeof(T));

        public static object GetValueOrEvaluateBinding(this TemplateScopeContext scope, object valueOrBinding, Type returnType)
        {
            if (valueOrBinding is string literal)
            {
                literal.ToStringSegment().ParseNextToken(out object value, out JsBinding binding);

                var oValue = binding != null
                    ? scope.EvaluateToken(binding)
                    : value;

                return oValue.ConvertTo(returnType);
            }
            
            return valueOrBinding.ConvertTo(returnType);
        }

        public static void TryGetPage(this TemplateScopeContext scope, string virtualPath, out TemplatePage page, out TemplateCodePage codePage)
        {
            scope.Context.TryGetPage(scope.PageResult.VirtualPath, virtualPath, out page, out codePage);
            codePage?.Init();
                
            var requiresRequest = codePage as IRequiresRequest;
            if (requiresRequest != null)
            {
                var request = scope.GetValue(TemplateConstants.Request) as IRequest;
                if (request != null)
                    requiresRequest.Request = request;
            }
        }
    }
}