using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Text;

namespace ServiceStack.Templates
{
    // ReSharper disable InconsistentNaming
    
    public partial class TemplateDefaultFilters
    {
        public List<object> step(IEnumerable target, object scopeOptions)
        {
            var items = target.AssertEnumerable(nameof(step));

            var scopedParams = scopeOptions.AssertOptions(nameof(step));

            var from = scopedParams.TryGetValue("from", out object oFrom)
                ? (int)oFrom
                : 0;

            var by = scopedParams.TryGetValue("by", out object oBy)
                ? (int)oBy
                : 1;

            var to = new List<object>();
            var itemsArray = items.ToArray();
            for (var i = from; i < itemsArray.Length; i += by)
            {
                to.Add(itemsArray[i]);
            }

            return to;
        }

        public object elementAt(IEnumerable target, int index)
        {
            var items = target.AssertEnumerable(nameof(elementAt));

            var i = 0;
            foreach (var item in items)
            {
                if (i++ == index)
                    return item;
            }

            return null;
        }

        public bool contains(object target, object needle)
        {
            if (isNull(needle))
                return false;

            if (target is string s)
            {
                if (needle is char c)
                    return s.IndexOf(c) >= 0;
                return s.IndexOf(needle.ToString(), StringComparison.Ordinal) >= 0;
            }
            if (target is IEnumerable items)
            {
                foreach (var item in items)
                {
                    if (Equals(item, needle))
                        return true;
                }
                return false;
            }
            throw new NotSupportedException($"'{nameof(contains)}' requires a string or IEnumerable but received a '{target.GetType()?.Name}' instead");
        }

        public IEnumerable<object> take(TemplateScopeContext scope, IEnumerable<object> original, object countOrBinding) =>
            original.Take(scope.GetValueOrEvaluateBinding<int>(countOrBinding));

        public IEnumerable<object> skip(TemplateScopeContext scope, IEnumerable<object> original, object countOrBinding) =>
            original.Skip(scope.GetValueOrEvaluateBinding<int>(countOrBinding));

        public IEnumerable<object> limit(TemplateScopeContext scope, IEnumerable<object> original, object skipOrBinding, object takeOrBinding)
        {
            var skip = scope.GetValueOrEvaluateBinding<int>(skipOrBinding);
            var take = scope.GetValueOrEvaluateBinding<int>(takeOrBinding);
            return original.Skip(skip).Take(take);
        }

        public int count(TemplateScopeContext scope, object target) => target.AssertEnumerable(nameof(count)).Count();
        public int count(TemplateScopeContext scope, object target, object expression) => count(scope, target, expression, null);
        public int count(TemplateScopeContext scope, object target, object expression, object scopeOptions)
        {
            var items = target.AssertEnumerable(nameof(count));
            var literal = scope.AssertExpression(nameof(count), expression);
            var scopedParams = scope.GetParamsWithItemBinding(nameof(count), scopeOptions, out string itemBinding);

            literal.ParseConditionExpression(out ConditionExpression expr);
            var total = 0;
            var i = 0;
            foreach (var item in items)
            {
                scope.AddItemToScope(itemBinding, item, i++);
                var result = expr.Evaluate(scope);
                if (result)
                    total++;
            }

            return total;
        }

        public object sum(TemplateScopeContext scope, object target) => sum(scope, target, null, null);
        public object sum(TemplateScopeContext scope, object target, object expression) => sum(scope, target, expression, null);
        public object sum(TemplateScopeContext scope, object target, object expression, object scopeOptions) =>
            applyInternal(nameof(sum), scope, target, expression, scopeOptions, (a, b) => a + b);

        public object min(TemplateScopeContext scope, object target) => min(scope, target, null, null);
        public object min(TemplateScopeContext scope, object target, object expression) => min(scope, target, expression, null);
        public object min(TemplateScopeContext scope, object target, object expression, object scopeOptions) =>
            applyInternal(nameof(min), scope, target, expression, scopeOptions, (a, b) => b < a ? b : a);

        public object max(TemplateScopeContext scope, object target) => max(scope, target, null, null);
        public object max(TemplateScopeContext scope, object target, object expression) => max(scope, target, expression, null);
        public object max(TemplateScopeContext scope, object target, object expression, object scopeOptions) =>
            applyInternal(nameof(max), scope, target, expression, scopeOptions, (a, b) => b > a ? b : a);

        public double average(TemplateScopeContext scope, object target) => average(scope, target, null, null);
        public double average(TemplateScopeContext scope, object target, object expression) => average(scope, target, expression, null);
        public double average(TemplateScopeContext scope, object target, object expression, object scopeOptions) =>
            applyInternal(nameof(average), scope, target, expression, scopeOptions, (a, b) => a + b).ConvertTo<double>() / target.AssertEnumerable(nameof(average)).Count();

        private object applyInternal(string filterName, TemplateScopeContext scope, object target, object expression, object scopeOptions,
            Func<double, double, double> fn)
        {
            if (target is double d)
                return fn(d, expression.ConvertTo<double>());
            if (target is int i)
                return (int)fn(i, expression.ConvertTo<double>());
            if (target is long l)
                return (long)fn(l, expression.ConvertTo<double>());

            var items = target.AssertEnumerable(filterName);
            var total = filterName == nameof(min)
                ? double.MaxValue
                : 0;
            Type itemType = null;
            if (expression != null)
            {
                var literal = scope.AssertExpression(filterName, expression);
                var scopedParams = scope.GetParamsWithItemBinding(filterName, scopeOptions, out string itemBinding);
                literal.ToStringSegment().ParseNextToken(out object value, out JsBinding binding);
                foreach (var item in items)
                {
                    if (item == null) continue;

                    scope.AddItemToScope(itemBinding, item);
                    var result = scope.Evaluate(value, binding);
                    if (result == null) continue;
                    if (itemType == null)
                        itemType = result.GetType();

                    total = fn(total, result.ConvertTo<double>());
                }
            }
            else
            {
                foreach (var item in items)
                {
                    if (item == null) continue;
                    if (itemType == null)
                        itemType = item.GetType();
                    total = fn(total, item.ConvertTo<double>());
                }
            }

            if (filterName == nameof(min) && itemType == null)
                return 0;

            if (expression == null && itemType == null)
                itemType = target.GetType().FirstGenericType()?.GetGenericArguments().FirstOrDefault();

            return itemType == null || itemType == typeof(double)
                ? total
                : total.ConvertTo(itemType);
        }

        public object reduce(TemplateScopeContext scope, object target, object expression) => reduce(scope, target, expression, null);
        public object reduce(TemplateScopeContext scope, object target, object expression, object scopeOptions)
        {
            var items = target.AssertEnumerable(nameof(reduce));
            Type itemType = null;

            var literal = scope.AssertExpression(nameof(reduce), expression);
            var scopedParams = scope.GetParamsWithItemBinding(nameof(reduce), scopeOptions, out string itemBinding);
            var accumulator = scopedParams.TryGetValue("initialValue", out object initialValue)
                ? initialValue.ConvertTo<double>()
                : 1;

            var bindAccumlator = scopedParams.TryGetValue("accumulator", out object accumulatorName)
                ? (string)accumulatorName
                : "accumulator";

            literal.ToStringSegment().ParseNextToken(out object value, out JsBinding binding);
            var i = 0;
            foreach (var item in items)
            {
                if (item == null) continue;

                scope.AddItemToScope(bindAccumlator, accumulator);
                scope.AddItemToScope("index", i++);
                scope.AddItemToScope(itemBinding, item);

                var result = scope.Evaluate(value, binding);
                if (result == null) continue;
                if (itemType == null)
                    itemType = result.GetType();

                accumulator = result.ConvertTo<double>();
            }

            if (expression == null && itemType == null)
                itemType = target.GetType().FirstGenericType()?.GetGenericArguments().FirstOrDefault();

            return itemType == null || itemType == typeof(double)
                ? accumulator
                : accumulator.ConvertTo(itemType);
        }

        public List<object[]> zip(TemplateScopeContext scope, IEnumerable original, object itemsOrBinding)
        {
            var to = new List<object[]>();

            if (itemsOrBinding is string literal)
            {
                literal.ToStringSegment().ParseNextToken(out object value, out JsBinding binding);

                var i = 0;
                foreach (var a in original)
                {
                    scope.AddItemToScope("it", a, i++);
                    var bindValue = scope.Evaluate(value, binding);
                    if (bindValue is IEnumerable current)
                    {
                        foreach (var b in current)
                        {
                            to.Add(new[] { a, b });
                        }
                    }
                    else if (bindValue != null)
                        throw new ArgumentException($"{nameof(zip)} in '{scope.Page.VirtualPath}' requires '{literal}' to evaluate to an IEnumerable, but evaluated to a '{bindValue.GetType().Name}' instead");
                }
            }
            else if (itemsOrBinding is IEnumerable current)
            {
                var currentArray = current.Cast<object>().ToArray();
                foreach (var a in original)
                {
                    foreach (var b in currentArray)
                    {
                        to.Add(new[] { a, b });
                    }
                }
            }

            return to;
        }

        public object let(TemplateScopeContext scope, object target, object scopeBindings) //from filter
        {
            var objs = target as IEnumerable;
            if (objs != null)
            {
                var scopedParams = scope.GetParamsWithItemBindingOnly(nameof(let), null, scopeBindings, out string itemBinding);

                var to = new List<ScopeVars>();
                var i = 0;
                foreach (var item in objs)
                {
                    scope.ScopedParams[TemplateConstants.Index] = i++;
                    scope.ScopedParams[itemBinding] = item;

                    // Copy over previous let bindings into new let bindings
                    var itemBindings = new ScopeVars();
                    if (item is object[] tuple)
                    {
                        foreach (var a in tuple)
                        {
                            if (a is IDictionary<string, object> aArgs)
                            {
                                foreach (var entry in aArgs)
                                {
                                    itemBindings[entry.Key] = entry.Value;
                                }
                            }
                        }
                    }

                    foreach (var entry in scopedParams)
                    {
                        var bindTo = entry.Key;
                        if (!(entry.Value is string bindToLiteral))
                            throw new NotSupportedException($"'{nameof(let)}' in '{scope.Page.VirtualPath}' expects a string Expression for its value but received '{entry.Value}' instead");

                        bindToLiteral.ToStringSegment().ParseNextToken(out object value, out JsBinding binding);
                        var bindValue = scope.Evaluate(value, binding);
                        scope.ScopedParams[bindTo] = bindValue;
                        itemBindings[bindTo] = bindValue;
                    }
                    to.Add(itemBindings);
                }

                return to;
            }
            if (target != null)
                throw new NotSupportedException($"'{nameof(let)}' in '{scope.Page.VirtualPath}' requires an IEnumerable but received a '{target.GetType()?.Name}' instead");

            return null;
        }

        public object first(TemplateScopeContext scope, object target) => target.AssertEnumerable(nameof(first)).FirstOrDefault();
        public object first(TemplateScopeContext scope, object target, object expression) => first(scope, target, expression, null);
        public object first(TemplateScopeContext scope, object target, object expression, object scopeOptions)
        {
            var items = target.AssertEnumerable(nameof(first));
            var literal = scope.AssertExpression(nameof(first), expression);
            var scopedParams = scope.GetParamsWithItemBinding(nameof(first), scopeOptions, out string itemBinding);

            literal.ParseConditionExpression(out ConditionExpression expr);
            var i = 0;
            foreach (var item in items)
            {
                scope.AddItemToScope(itemBinding, item, i++);
                var result = expr.Evaluate(scope);
                if (result)
                    return item;
            }

            return null;
        }

        public bool any(TemplateScopeContext scope, object target) => target.AssertEnumerable(nameof(any)).Any();
        public bool any(TemplateScopeContext scope, object target, object expression) => any(scope, target, expression, null);
        public bool any(TemplateScopeContext scope, object target, object expression, object scopeOptions)
        {
            var items = target.AssertEnumerable(nameof(any));
            var literal = scope.AssertExpression(nameof(any), expression);
            var scopedParams = scope.GetParamsWithItemBinding(nameof(any), scopeOptions, out string itemBinding);

            literal.ParseConditionExpression(out ConditionExpression expr);
            var i = 0;
            foreach (var item in items)
            {
                scope.AddItemToScope(itemBinding, item, i++);
                var result = expr.Evaluate(scope);
                if (result)
                    return true;
            }

            return false;
        }

        public bool all(TemplateScopeContext scope, object target, object expression) => all(scope, target, expression, null);
        public bool all(TemplateScopeContext scope, object target, object expression, object scopeOptions)
        {
            var items = target.AssertEnumerable(nameof(all));
            var literal = scope.AssertExpression(nameof(all), expression);
            var scopedParams = scope.GetParamsWithItemBinding(nameof(where), scopeOptions, out string itemBinding);

            literal.ParseConditionExpression(out ConditionExpression expr);
            var i = 0;
            foreach (var item in items)
            {
                scope.AddItemToScope(itemBinding, item, i++);
                var result = expr.Evaluate(scope);
                if (!result)
                    return false;
            }

            return true;
        }

        public IEnumerable<object> where(TemplateScopeContext scope, object target, object expression) => where(scope, target, expression, null);
        public IEnumerable<object> where(TemplateScopeContext scope, object target, object expression, object scopeOptions)
        {
            var items = target.AssertEnumerable(nameof(where));
            var literal = scope.AssertExpression(nameof(where), expression);
            var scopedParams = scope.GetParamsWithItemBinding(nameof(where), scopeOptions, out string itemBinding);

            var to = new List<object>();
            literal.ParseConditionExpression(out ConditionExpression expr);
            var i = 0;
            foreach (var item in items)
            {
                scope.AddItemToScope(itemBinding, item, i++);
                var result = expr.Evaluate(scope);
                if (result)
                    to.Add(item);
            }

            return to;
        }

        public IEnumerable<object> takeWhile(TemplateScopeContext scope, object target, object expression) => takeWhile(scope, target, expression, null);
        public IEnumerable<object> takeWhile(TemplateScopeContext scope, object target, object expression, object scopeOptions)
        {
            var items = target.AssertEnumerable(nameof(takeWhile));
            var literal = scope.AssertExpression(nameof(takeWhile), expression);
            var scopedParams = scope.GetParamsWithItemBinding(nameof(takeWhile), scopeOptions, out string itemBinding);

            var to = new List<object>();
            literal.ParseConditionExpression(out ConditionExpression expr);
            var i = 0;
            foreach (var item in items)
            {
                scope.AddItemToScope(itemBinding, item, i++);
                var result = expr.Evaluate(scope);
                if (result)
                    to.Add(item);
                else
                    return to;
            }

            return to;
        }

        public IEnumerable<object> skipWhile(TemplateScopeContext scope, object target, object expression) => skipWhile(scope, target, expression, null);
        public IEnumerable<object> skipWhile(TemplateScopeContext scope, object target, object expression, object scopeOptions)
        {
            var items = target.AssertEnumerable(nameof(skipWhile));
            var literal = scope.AssertExpression(nameof(skipWhile), expression);
            var scopedParams = scope.GetParamsWithItemBinding(nameof(skipWhile), scopeOptions, out string itemBinding);

            var to = new List<object>();
            literal.ParseConditionExpression(out ConditionExpression expr);
            var i = 0;
            var keepSkipping = true;
            foreach (var item in items)
            {
                scope.AddItemToScope(itemBinding, item, i++);
                var result = expr.Evaluate(scope);
                if (!result)
                    keepSkipping = false;

                if (!keepSkipping)
                    to.Add(item);
            }

            return to;
        }

        public IEnumerable<object> orderBy(TemplateScopeContext scope, object target, object expression) => orderBy(scope, target, expression, null);
        public IEnumerable<object> orderBy(TemplateScopeContext scope, object target, object expression, object scopeOptions) =>
            orderByInternal(nameof(orderBy), scope, target, expression, scopeOptions);

        public IEnumerable<object> orderByDescending(TemplateScopeContext scope, object target, object expression) => orderByDescending(scope, target, expression, null);
        public IEnumerable<object> orderByDescending(TemplateScopeContext scope, object target, object expression, object scopeOptions) =>
            orderByInternal(nameof(orderByDescending), scope, target, expression, scopeOptions);

        public IEnumerable<object> orderByDesc(TemplateScopeContext scope, object target, object expression) => orderByDesc(scope, target, expression, null);
        public IEnumerable<object> orderByDesc(TemplateScopeContext scope, object target, object expression, object scopeOptions) =>
            orderByInternal(nameof(orderByDescending), scope, target, expression, scopeOptions);

        public IEnumerable<object> thenBy(TemplateScopeContext scope, object target, object expression) => thenBy(scope, target, expression, null);
        public IEnumerable<object> thenBy(TemplateScopeContext scope, object target, object expression, object scopeOptions) =>
            thenByInternal(nameof(thenBy), scope, target, expression, scopeOptions);

        public IEnumerable<object> thenByDescending(TemplateScopeContext scope, object target, object expression) => thenByDescending(scope, target, expression, null);
        public IEnumerable<object> thenByDescending(TemplateScopeContext scope, object target, object expression, object scopeOptions) =>
            thenByInternal(nameof(thenByDescending), scope, target, expression, scopeOptions);

        class ComparerWrapper : IComparer<object>
        {
            private readonly IComparer comparer;
            public ComparerWrapper(IComparer comparer) => this.comparer = comparer;
            public int Compare(object x, object y) => comparer.Compare(x, y);
        }
        class EqualityComparerWrapper : IEqualityComparer<object>
        {
            private readonly IEqualityComparer comparer;
            public EqualityComparerWrapper(IEqualityComparer comparer) => this.comparer = comparer;
            public new bool Equals(object x, object y) => comparer.Equals(x, y);
            public int GetHashCode(object obj) => comparer.GetHashCode(obj);
        }
        class EqualityComparerWrapper<T> : IEqualityComparer<object>
        {
            private readonly IEqualityComparer<T> comparer;
            public EqualityComparerWrapper(IEqualityComparer<T> comparer) => this.comparer = comparer;
            public new bool Equals(object x, object y) => comparer.Equals((T)x, (T)y);
            public int GetHashCode(object obj) => comparer.GetHashCode((T)obj);
        }

        private static IComparer<object> GetComparer(string filterName, TemplateScopeContext scope, Dictionary<string, object> scopedParams)
        {
            var comparer = (IComparer<object>)Comparer<object>.Default;
            if (scopedParams.TryGetValue(TemplateConstants.Comparer, out object oComparer))
            {
                var nonGenericComparer = oComparer as IComparer;
                if (nonGenericComparer == null)
                    throw new NotSupportedException(
                        $"'{filterName}' in '{scope.Page.VirtualPath}' expects a IComparer but received a '{oComparer.GetType()?.Name}' instead");
                comparer = new ComparerWrapper(nonGenericComparer);
            }
            return comparer;
        }

        public static IEnumerable<object> orderByInternal(string filterName, TemplateScopeContext scope, object target, object expression, object scopeOptions)
        {
            var items = target.AssertEnumerable(filterName);
            var literal = scope.AssertExpression(filterName, expression);
            var scopedParams = scope.GetParamsWithItemBinding(filterName, scopeOptions, out string itemBinding);

            literal.ToStringSegment().ParseNextToken(out object value, out JsBinding binding);
            var i = 0;

            var comparer = GetComparer(filterName, scope, scopedParams);

            var sorted = filterName == nameof(orderByDescending)
                ? items.OrderByDescending(item => scope.AddItemToScope(itemBinding, item, i++).Evaluate(value, binding), comparer)
                : items.OrderBy(item => scope.AddItemToScope(itemBinding, item, i++).Evaluate(value, binding), comparer);

            return sorted;
        }

        public static IEnumerable<object> thenByInternal(string filterName, TemplateScopeContext scope, object target, object expression, object scopeOptions)
        {
            var items = target as IOrderedEnumerable<object>;
            if (items == null)
                throw new NotSupportedException($"'{filterName}' in '{scope.Page.VirtualPath}' requires an IOrderedEnumerable but received a '{target?.GetType()?.Name}' instead");

            var literal = scope.AssertExpression(filterName, expression);
            var scopedParams = scope.GetParamsWithItemBinding(filterName, scopeOptions, out string itemBinding);

            literal.ToStringSegment().ParseNextToken(out object value, out JsBinding binding);
            var i = 0;

            var comparer = GetComparer(filterName, scope, scopedParams);

            var sorted = filterName == nameof(thenByDescending)
                ? items.ThenByDescending(item => scope.AddItemToScope(itemBinding, item, i++).Evaluate(value, binding), comparer)
                : items.ThenBy(item => scope.AddItemToScope(itemBinding, item, i++).Evaluate(value, binding), comparer);

            return sorted;
        }

        public IEnumerable<IGrouping<object, object>> groupBy(TemplateScopeContext scope, IEnumerable<object> items, object expression) => groupBy(scope, items, expression, null);
        public IEnumerable<IGrouping<object, object>> groupBy(TemplateScopeContext scope, IEnumerable<object> items, object expression, object scopeOptions)
        {
            var literal = scope.AssertExpression(nameof(groupBy), expression);
            var scopedParams = scope.GetParamsWithItemBinding(nameof(groupBy), scopeOptions, out string itemBinding);

            literal.ToStringSegment().ParseNextToken(out object value, out JsBinding binding);

            var comparer = (IEqualityComparer<object>)EqualityComparer<object>.Default;
            if (scopedParams.TryGetValue(TemplateConstants.Comparer, out object oComparer))
            {
                comparer = oComparer as IEqualityComparer<object>;
                if (oComparer is IEqualityComparer<string> stringComparer)
                    comparer = new EqualityComparerWrapper<string>(stringComparer);
                else if (oComparer is IEqualityComparer<int> intComparer)
                    comparer = new EqualityComparerWrapper<int>(intComparer);
                else if (oComparer is IEqualityComparer<long> longComparer)
                    comparer = new EqualityComparerWrapper<long>(longComparer);
                else if (oComparer is IEqualityComparer<double> doubleComparer)
                    comparer = new EqualityComparerWrapper<double>(doubleComparer);

                if (comparer == null)
                {
                    var nonGenericComparer = oComparer as IEqualityComparer;
                    if (nonGenericComparer == null)
                        throw new NotSupportedException(
                            $"'{nameof(groupBy)}' in '{scope.Page.VirtualPath}' expects a IEqualityComparer but received a '{oComparer.GetType()?.Name}' instead");
                    comparer = new EqualityComparerWrapper(nonGenericComparer);
                }
            }

            if (scopedParams.TryGetValue(TemplateConstants.Map, out object map))
            {
                ((string)map).ToStringSegment().ParseNextToken(out object mapValue, out JsBinding mapBinding);

                var result = items.GroupBy(
                    item => scope.AddItemToScope(itemBinding, item).Evaluate(value, binding),
                    item => scope.AddItemToScope(itemBinding, item).Evaluate(mapValue, mapBinding),
                    comparer);
                return result;
            }
            else
            {
                var result = items.GroupBy(item => scope.AddItemToScope(itemBinding, item).Evaluate(value, binding), comparer);
                return result;
            }
        }

        public IEnumerable<object> distinct(IEnumerable<object> items) => items.Distinct();
        public IEnumerable<object> union(IEnumerable<object> target, IEnumerable<object> items) => target.Union(items);
        public IEnumerable<object> concat(IEnumerable<object> target, IEnumerable<object> items) => target.Concat(items);
        public IEnumerable<object> intersect(IEnumerable<object> target, IEnumerable<object> items) => target.Intersect(items);
        public IEnumerable<object> except(IEnumerable<object> target, IEnumerable<object> items) => target.Except(items);
        public bool equivalentTo(IEnumerable<object> target, IEnumerable<object> items) => target.EquivalentTo(items);

        public object get(object target, object key)
        {
            if (isNull(target))
                return null;

            if (target is IDictionary d)
            {
                if (d.Contains(key))
                    return d[key];
            }
            else if (target is object[] a)
            {
                var index = key.ConvertTo<int>();
                return index < a.Length
                    ? a[index]
                    : null;
            }
            else if (target is IList l)
            {
                var index = key.ConvertTo<int>();
                return index < l.Count
                    ? l[index]
                    : null;
            }
            else if (target is IEnumerable e)
            {
                var index = key.ConvertTo<int>();
                var i = 0;
                foreach (var value in e)
                {
                    if (i++ == index)
                        return value;
                }
            }

            throw new NotSupportedException($"'{nameof(get)}' expects a collection but received a '{target.GetType().Name}'");
        }
    }
}