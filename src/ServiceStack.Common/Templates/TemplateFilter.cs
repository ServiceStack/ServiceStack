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
        public static readonly IgnoreResult Value = new IgnoreResult();
        private IgnoreResult(){}
    }

    public class StopExecution : IResultInstruction
    {
        public static StopExecution Value = new StopExecution();
        private StopExecution() { }
    }

    public class StopFilterExecutionException : StopExecutionException
    {
        public TemplateScopeContext Scope { get; }
        public object Options { get; }

        public StopFilterExecutionException(TemplateScopeContext scope, object options, Exception innerException) 
            : base(nameof(StopFilterExecutionException), innerException)
        {
            Scope = scope;
            Options = options;
        }
    }

    public class TemplateFilter
    {
        public TemplateContext Context { get; set; }
        public ITemplatePages Pages { get; set; }
        
        private readonly Dictionary<string, MethodInfo> lookupIndex = new Dictionary<string, MethodInfo>();

        public TemplateFilter()
        {
            var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
            foreach (var method in methods)
            {
                var hasScope = method.GetParameters().FirstOrDefault()?.ParameterType == typeof(TemplateScopeContext);
                var hasTaskReturnType = method.ReturnType == typeof(Task);
                var isFilter = !hasScope && !hasTaskReturnType;
                var isContextFilter = hasScope && !hasTaskReturnType;
                var isBlockFilter = hasScope && hasTaskReturnType;
                if (!isFilter && !isContextFilter && !isBlockFilter)
                    continue;

                var filterType = isBlockFilter
                    ? InvokerType.ContextBlock
                    : isContextFilter
                        ? InvokerType.ContextFilter
                        : InvokerType.Filter;

                var key = CacheKey(filterType, method.Name, method.GetParameters().Length);
                lookupIndex[key] = method;
            }
        }

        private string CacheKey(InvokerType type, string methodName, int argsCount) =>
            type + "::" + methodName.ToLower() + "`" + argsCount;

        private MethodInfo GetFilterMethod(string cacheKey) => lookupIndex.TryGetValue(cacheKey, out MethodInfo method) ? method : null;

        public virtual bool HandlesUnknownValue(string name, int argsCount)
        {
            var method = GetFilterMethod(CacheKey(InvokerType.Filter, name, argsCount))
                ?? GetFilterMethod(CacheKey(InvokerType.ContextFilter, name, argsCount + 1))
                ?? GetFilterMethod(CacheKey(InvokerType.ContextBlock, name, argsCount + 1));
            return method != null && method.HasAttribute<HandleUnknownValueAttribute>();
        }

        public List<MethodInfo> QueryFilters(string filterName)
        {
            var filters = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.Name.IndexOf(filterName, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
            return filters;
        }

        public ConcurrentDictionary<string, MethodInvoker> InvokerCache { get; } = new ConcurrentDictionary<string, MethodInvoker>();

        public MethodInvoker GetInvoker(string name, int argsCount, InvokerType type)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            var key = CacheKey(type, name, argsCount);
            if (InvokerCache.TryGetValue(key, out MethodInvoker invoker))
                return invoker;

            var method = GetFilterMethod(key);
            if (method == null)
                return null;

            return InvokerCache[key] = TypeExtensions.GetInvokerToCache(method);
        }
    }

    public static class TemplateFilterUtils
    {
        public static Dictionary<string, object> AssertOptions(this TemplateScopeContext scope, string filterName, object scopedParams)
        {
            var pageParams = scopedParams as Dictionary<string, object>;
            if (pageParams == null && scopedParams != null)
                throw new ArgumentException(
                    $"{filterName} in '{scope.PageResult.VirtualPath}' only accepts an Object dictionary as an argument but received a '{scopedParams.GetType().Name}' instead");

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
                throw new NotSupportedException($"'{filterName}' in '{scope.PageResult.VirtualPath}' requires a string Expression but received a '{expression?.GetType()?.Name}' instead");
            return literal;
        }

        public static JsToken AssertExpression(this TemplateScopeContext scope, string filterName, object expression, object scopeOptions, out string itemBinding)
        {
            if (expression is JsArrowFunctionExpression arrowExpr)
            {
                itemBinding = arrowExpr.Params[0].Name;
                return arrowExpr.Body;
            }
            
            var literal = scope.AssertExpression(filterName, expression);
            var scopedParams = scope.GetParamsWithItemBinding(filterName, scopeOptions, out itemBinding);

            var token = literal.GetCachedJsExpression(scope);
            return token;
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

            if (item is ScopeVars explodeBindings)
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
                literal.ParseJsExpression(out var token);
                var oValue = token.Evaluate(scope);
                return oValue.ConvertTo(returnType);
            }
            
            return valueOrBinding.ConvertTo(returnType);
        }

        public static bool TryGetPage(this TemplateScopeContext scope, string virtualPath, out TemplatePage page, out TemplateCodePage codePage)
        {
            if (scope.PageResult.Partials.TryGetValue(virtualPath, out page))
            {
                codePage = null;
                return true;
            }
            
            if (!scope.Context.TryGetPage(scope.PageResult.VirtualPath, virtualPath, out page, out codePage))
                return false;
            
            codePage?.Init();

            if (codePage is IRequiresRequest requiresRequest)
            {
                if (scope.GetValue(TemplateConstants.Request) is IRequest request)
                    requiresRequest.Request = request;
            }

            return true;
        }
        
        
        public static TemplateContext CreateNewContext(this TemplateScopeContext scope, Dictionary<string, object> args)
        {
            if (args == null)
                return new TemplateContext().Init();
            
            var context = new TemplateContext();
            if (args.TryGetValue("use", out var oUse))
            {
                var use = (Dictionary<string, object>) oUse;
                if (use.TryGetValue("context", out var oContext) && oContext is bool useContext && useContext)
                {
                    return scope.Context;
                }

                // Use same ThreadSafe plugin instance to preserve configuration 
                var plugins = use.TryGetValue("plugins", out var oPlugins)
                    ? ToStrings("plugins", oPlugins)
                    : null;
                if (plugins != null)
                {
                    foreach (var name in plugins)
                    {
                        var plugin = scope.Context.Plugins.FirstOrDefault(x => x.GetType().Name == name);
                        if (plugin == null)
                            throw new NotSupportedException($"Plugin '{name}' is not registered in parent context");
                            
                        context.Plugins.Add(plugin);
                    }
                }

                // Use new filter and block instances which cannot be shared between contexts
                var filters = use.TryGetValue("filters", out var oFilters)
                    ? ToStrings("filters", oFilters)
                    : null;
                if (filters != null)
                {
                    foreach (var name in filters)
                    {
                        var filter = scope.Context.TemplateFilters.FirstOrDefault(x => x.GetType().Name == name);
                        if (filter == null)
                            throw new NotSupportedException($"Filter '{name}' is not registered in parent context");
                            
                        context.TemplateFilters.Add(filter.GetType().CreateInstance<TemplateFilter>());
                    }
                }
                    
                var blocks = use.TryGetValue("blocks", out var oBlocks)
                    ? ToStrings("blocks", oBlocks)
                    : null;
                if (blocks != null)
                {
                    foreach (var name in blocks)
                    {
                        var useBlock = scope.Context.TemplateBlocks.FirstOrDefault(x => x.GetType().Name == name);
                        if (useBlock == null)
                            throw new NotSupportedException($"Block '{name}' is not registered in parent context");
                            
                        context.TemplateBlocks.Add(useBlock.GetType().CreateInstance<TemplateBlock>());
                    }
                }

                args.Remove(nameof(use));
            }
            context.Init();

            return context;
        }

        private static IEnumerable<string> ToStrings(string argName, object arg)
        {
            if (arg == null)
                return null;
            
            var strings = arg is IEnumerable<string> ls
                ? ls
                : arg is string s
                    ? new List<string> {s}
                    : arg is IEnumerable<object> e
                        ? e.Map(x => (string) x)
                        : throw new NotSupportedException($"{argName} expected a collection of strings but was '{arg.GetType().Name}'");

            return strings;
        }
        
    }
}