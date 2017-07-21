using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Text.Controller;
using ServiceStack.Text.Json;

namespace ServiceStack.Templates
{
    // ReSharper disable InconsistentNaming
    
    public interface IResultInstruction {}
    public class IgnoreResult : IResultInstruction
    {
        internal static readonly IgnoreResult Value = new IgnoreResult();
        private IgnoreResult(){}
    }

    public class TemplateDefaultFilters : TemplateFilter
    {
        public static TemplateDefaultFilters Instance = new TemplateDefaultFilters();
        
        // methods without arguments can be used in bindings, e.g. {{ now | dateFormat }}
        public DateTime now() => DateTime.Now;
        public DateTime utcNow() => DateTime.UtcNow;
        
        public string indent() => Context.Args[TemplateConstants.DefaultIndent] as string;
        public string indents(int count) => repeat(Context.Args[TemplateConstants.DefaultIndent] as string, count);
        public string space() => " ";
        public string spaces(int count) => padLeft("", count, ' ');
        public string newLine() => Context.Args[TemplateConstants.DefaultNewLine] as string;
        public string newLines(int count) => repeat(newLine(), count);

        public IRawString raw(object value)
        {
            if (value == null || Equals(value, JsNull.Value))
                return TemplateConstants.EmptyRawString;
            if (value is string s)
                return s == string.Empty ? TemplateConstants.EmptyRawString : s.ToRawString();
            if (value is IRawString r)
                return r;
            if (value is bool b)
                return b ? TemplateConstants.TrueRawString : TemplateConstants.FalseRawString;
            
            var rawStr = value.ToString().ToRawString();
            return rawStr;
        }

        public string appSetting(string name) =>  Context.AppSettings.GetString(name);

        public static double applyToNumbers(double lhs, double rhs, Func<double, double, double> fn) => fn(lhs, rhs);
        public double add(double lhs, double rhs) => applyToNumbers(lhs, rhs, (x, y) => x + y);
        public double sub(double lhs, double rhs) => applyToNumbers(lhs, rhs, (x, y) => x - y);
        public double subtract(double lhs, double rhs) => applyToNumbers(lhs, rhs, (x, y) => x - y);
        public double mul(double lhs, double rhs) => applyToNumbers(lhs, rhs, (x, y) => x * y);
        public double multiply(double lhs, double rhs) => applyToNumbers(lhs, rhs, (x, y) => x * y);
        public double div(double lhs, double rhs) => applyToNumbers(lhs, rhs, (x, y) => x / y);
        public double divide(double lhs, double rhs) => applyToNumbers(lhs, rhs, (x, y) => x / y);

        public long incr(long value) => value + 1; 
        public long increment(long value) => value + 1; 
        public long incrBy(long value, long by) => value + by; 
        public long incrementBy(long value, long by) => value + by; 
        public long decr(long value) => value - 1; 
        public long decrement(long value) => value - 1; 
        public long decrBy(long value, long by) => value - by; 
        public long decrementBy(long value, long by) => value - by;
        public long mod(long value, long divisor) => 
            value % divisor; 

        public bool isEven(int value) => value % 2 == 0;
        public bool isOdd(int value) => !isEven(value);

        public string currency(decimal decimalValue) => currency(decimalValue, null); //required to support 1/2 vars
        public string currency(decimal decimalValue, string culture)
        {
            var cultureInfo = culture != null
                ? new CultureInfo(culture)
                : (CultureInfo) Context.Args[TemplateConstants.DefaultCulture];

            var fmt = string.Format(cultureInfo, "{0:C}", decimalValue);
            return fmt;
        }

        public string format(object obj, string format) => obj is IFormattable formattable 
            ? formattable.ToString(format, null) 
            : string.Format(format, obj);

        public object dateFormat(DateTime dateValue) =>  dateValue.ToString((string)Context.Args[TemplateConstants.DefaultDateFormat]);
        public object dateFormat(DateTime dateValue, string format) => dateValue.ToString(format ?? throw new ArgumentNullException(nameof(format)));
        public object dateTimeFormat(DateTime dateValue) =>  dateValue.ToString((string)Context.Args[TemplateConstants.DefaultDateTimeFormat]);

        public string humanize(string varName) => varName.SplitCamelCase().Replace('_',' ').ToTitleCase();
        public string titleCase(string varName) => varName.ToTitleCase();
        public string pascalCase(string varName) => varName.ToPascalCase();
        public string camelCase(string varName) => varName.ToCamelCase();

        public string lower(string varName) => varName?.ToLower();
        public string upper(string varName) => varName?.ToUpper();

        public string substring(string varName, int startIndex) => varName.SafeSubstring(startIndex);
        public string substring(string varName, int startIndex, int length) => varName.SafeSubstring(startIndex, length);

        public string trimStart(string text) => text?.TrimStart();
        public string trimEnd(string text) => text?.TrimEnd();
        public string trim(string text) => text?.Trim();

        public string padLeft(string text, int totalWidth) => text?.PadLeft(totalWidth);
        public string padLeft(string text, int totalWidth, char padChar) => text?.PadLeft(totalWidth, padChar);
        public string padRight(string text, int totalWidth) => text?.PadRight(totalWidth);
        public string padRight(string text, int totalWidth, char padChar) => text?.PadRight(totalWidth, padChar);

        public string repeating(int times, string text) => repeat(text, times);
        public string repeat(string text, int times)
        {
            var sb = StringBuilderCache.Allocate();
            for (var i = 0; i < times; i++)
            {
                sb.Append(text);
            }
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public List<object> itemsOf(int count, object target)
        {
            var to = new List<object>();
            for (var i = 0; i < count; i++)
            {
                to.Add(target);
            }
            return to;
        }
        
        public static bool isTrue(object target) => target is bool b && b;
        public static bool isFalsey(object target)
        {
            if (target == null || target == JsNull.Value)
                return true;
            if (target is string s)
                return string.IsNullOrEmpty(s);
            if (target is bool b)
                return !b;
            if (target is int i)
                return i == 0;
            if (target is long l)
                return l == 0;
            if (target is double d)
                return d == 0 || double.IsNaN(d);
            
            return false;
        }

        public object @if(object returnTarget, object test) => isTrue(test) ? returnTarget : null;
        public object iif(object test, object ifTrue, object ifFalse) => isTrue(test) ? ifTrue : ifFalse;
        public object when(object returnTarget, object test) => @if(returnTarget, test);     //alias

        public object ifNot(object returnTarget, object test) => !isTrue(test) ? returnTarget : null;
        public object unless(object returnTarget, object test) => ifNot(returnTarget, test); //alias
        
        [HandleUnknownValue]
        public object otherwise(object returnTaget, object elseReturn) => returnTaget ?? elseReturn;

        [HandleUnknownValue]
        public object ifFalsey(object returnTarget, object test) => isFalsey(test) ? returnTarget : null;

        [HandleUnknownValue]
        public object ifTruthy(object returnTarget, object test) => !isFalsey(test) ? returnTarget : null;
        
        [HandleUnknownValue]
        public object falsy(object test, object returnIfFalsy) => isFalsey(test) ? returnIfFalsy : null;

        [HandleUnknownValue]
        public object truthy(object test, object returnIfTruthy) => !isFalsey(test) ? returnIfTruthy : null;

        public bool or(object lhs, object rhs) => isTrue(lhs) || isTrue(rhs);
        public bool and(object lhs, object rhs) => isTrue(lhs) && isTrue(rhs);

        public bool equals(object target, object other) =>
            target == null || other == null 
                ? target == other 
                : target.GetType() == other.GetType() 
                    ? target.Equals(other)
                    : target.Equals(other.ConvertTo(target.GetType()));

        public bool notEquals(object target, object other) => !equals(target, other);

        public bool greaterThan(object target, object other) => compareTo(target, other, i => i > 0);
        public bool greaterThanEqual(object target, object other) => compareTo(target, other, i => i >= 0);
        public bool lessThan(object target, object other) => compareTo(target, other, i => i < 0);
        public bool lessThanEqual(object target, object other) => compareTo(target, other, i => i <= 0);

        //aliases
        public bool eq(object target, object other) => equals(target, other);
        public bool not(object target, object other) => notEquals(target, other);
        public bool gt(object target, object other) => greaterThan(target, other);
        public bool gte(object target, object other) => greaterThanEqual(target, other);
        public bool lt(object target, object other) => lessThan(target, other);
        public bool lte(object target, object other) => lessThanEqual(target, other);

        internal static bool compareTo(object target, object other, Func<int, bool> fn)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            
            if (target is IComparable c)
            {
                return target.GetType() == other?.GetType() 
                    ? fn(c.CompareTo(other))
                    : fn(c.CompareTo(other.ConvertTo(target.GetType())));
            }
            
            throw new NotSupportedException($"{target} is not IComparable");
        }

        public object echo(object value) => value;

        public IEnumerable join(IEnumerable<object> values) => join(values, ",");
        public IEnumerable join(IEnumerable<object> values, string delimiter) => values.Map(x => x.ToString()).Join(delimiter);

        public string[] split(string stringList) => split(stringList, ',');
        public string[] split(string stringList, char delimiter) => stringList.Split(delimiter);

        public string append(string target, string suffix) => target + suffix;
        public string appendLine(string target) => target + newLine();
        public string newLine(string target) => target + newLine();

        public string addPath(string target, string pathToAppend) => target.AppendPath(pathToAppend);
        public string addPaths(string target, IEnumerable pathsToAppend) => 
            target.AppendPath(pathsToAppend.Map(x => x.ToString()).ToArray());

        public string addQueryString(string url, object urlParams) => 
            urlParams.AssertOptions(nameof(addQueryString)).Aggregate(url, (current, entry) => current.AddQueryParam(entry.Key, entry.Value));
        
        public string addHashParams(string url, object urlParams) => 
            urlParams.AssertOptions(nameof(addHashParams)).Aggregate(url, (current, entry) => current.AddHashParam(entry.Key, entry.Value));
        
        public IEnumerable<object> reverse(TemplateScopeContext scope, IEnumerable<object> original) => original.Reverse();

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
                        to.Add(new[]{ a, b });
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

                var to = new List<Dictionary<string, object>>();
                var i = 0;
                foreach (var item in objs)
                {
                    scope.ScopedParams[TemplateConstants.Index] = i++;
                    scope.ScopedParams[itemBinding] = item;

                    // Copy over previous let bindings into new let bindings
                    var itemBindings = new Dictionary<string, object>();
                    if (item is object[] tuple)
                    {
                        foreach (var a in tuple)
                        {
                            if (a is Dictionary<string, object> aArgs)
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
                        var bindToLiteral = (string)entry.Value;
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

        public object assign(TemplateScopeContext scope, string argExpr, object value) //from filter
        {
            var targetEndPos = argExpr.IndexOfAny(new[] {'.', '['});
            if (targetEndPos == -1)
            {
                scope.ScopedParams[argExpr] = value;
            }
            else
            {
                var targetName = argExpr.Substring(0, targetEndPos);
                if (!scope.ScopedParams.TryGetValue(targetName, out object target))
                    throw new NotSupportedException($"Cannot assign to non-existing '{targetName}' in {argExpr}");

                scope.InvokeAssignExpression(argExpr, target, value);
            }
            
            return value;
        }

        public Task assignTo(TemplateScopeContext scope, object value, string argName) //from filter
        {
            scope.ScopedParams[argName] = value;
            return TypeConstants.EmptyTask;
        }

        public Task assignTo(TemplateScopeContext scope, string argName) //from context filter
        {
            var ms = (MemoryStream) scope.OutputStream;
            var value = ms.ReadFully().FromUtf8Bytes();
            scope.ScopedParams[argName] = value;
            ms.SetLength(0); //just capture output, don't write anything to the ResponseStream
            return TypeConstants.EmptyTask;
        }

        public Task partial(TemplateScopeContext scope, object target) => partial(scope, target, null);
        public async Task partial(TemplateScopeContext scope, object target, object scopedParams)
        {
            var pageName = target.ToString();
            var pageParams = scope.AssertOptions(nameof(partial), scopedParams);

            var page = await scope.Context.GetPage(pageName).Init();
            await scope.WritePageAsync(page, pageParams);
        }

        public Task forEach(TemplateScopeContext scope, object target, object items) => forEach(scope, target, items, null);
        public async Task forEach(TemplateScopeContext scope, object target, object items, object scopeOptions) 
        {
            var objs = items as IEnumerable;
            if (objs != null)
            {
                var scopedParams = scope.GetParamsWithItemBinding(nameof(select), scopeOptions, out string itemBinding);
                
                var i = 0;
                var itemScope = scope.CreateScopedContext(target.ToString(), scopedParams);
                foreach (var item in objs)
                {
                    itemScope.AddItemToScope(itemBinding, item, i++);
                    await itemScope.WritePageAsync();
                }
            }
            else if (items != null)
            {
                throw new ArgumentException($"{nameof(forEach)} in '{scope.Page.VirtualPath}' requires an IEnumerable, but received a '{items.GetType().Name}' instead");
            }
        }

        public string toString(object target) => target?.ToString();
        public List<object> toList(IEnumerable target) => target.Map(x => x);
        public object[] toArray(IEnumerable target) => target.Map(x => x).ToArray();
        
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
            if (needle == null)
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

        public object times(int count) => count.Times().ToList();
        public object range(int count) => Enumerable.Range(0, count);
        public object range(int start, int count) => Enumerable.Range(start, count);

        public Dictionary<object, object> toDictionary(TemplateScopeContext scope, object target, object expression) => toDictionary(scope, target, expression, null);
        public Dictionary<object, object> toDictionary(TemplateScopeContext scope, object target, object expression, object scopeOptions)
        {
            var items = target.AssertEnumerable(nameof(toDictionary));
            var literal = scope.AssertExpression(nameof(toDictionary), expression);
            var scopedParams = scope.GetParamsWithItemBinding(nameof(toDictionary), scopeOptions, out string itemBinding);
            
            literal.ToStringSegment().ParseNextToken(out object value, out JsBinding binding);
            
            return items.ToDictionary(item => scope.AddItemToScope(itemBinding, item).Evaluate(value, binding));
        }

        public IEnumerable of(TemplateScopeContext scope, IEnumerable target, object scopeOptions)
        {
            var items = target.AssertEnumerable(nameof(of));
            var scopedParams = scope.GetParamsWithItemBinding(nameof(of), scopeOptions, out string itemBinding);

            if (scopedParams.TryGetValue("type", out object oType))
            {
                if (oType is string typeName)
                    return items.Where(x => x?.GetType()?.Name == typeName);
                if (oType is Type type)
                    return items.Where(x => x?.GetType() == type);
            }

            return items;
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
                return (int) fn(i, expression.ConvertTo<double>());
            if (target is long l)
                return (long) fn(l, expression.ConvertTo<double>());
            
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
                ? (string) accumulatorName
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

        public Task @do(TemplateScopeContext scope, object target, object expression) => @do(scope, target, expression, null);
        public Task @do(TemplateScopeContext scope, object target, object expression, object scopeOptions)
        {
            var items = target.AssertEnumerable(nameof(@do));
            var literal = scope.AssertExpression(nameof(@do), expression);
            var scopedParams = scope.GetParamsWithItemBinding(nameof(first), scopeOptions, out string itemBinding);

            literal.ToStringSegment().ParseNextToken(out object value, out JsBinding binding);
            var i = 0;
            foreach (var item in items)
            {
                scope.AddItemToScope(itemBinding, item, i++);
                var result = scope.Evaluate(value, binding);
            }

            return TypeConstants.EmptyTask;
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

        public object any(TemplateScopeContext scope, object target) => target.AssertEnumerable(nameof(any)).Any();
        public object any(TemplateScopeContext scope, object target, object expression) => any(scope, target, expression, null);
        public object any(TemplateScopeContext scope, object target, object expression, object scopeOptions)
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

        public object all(TemplateScopeContext scope, object target, object expression) => all(scope, target, expression, null);
        public object all(TemplateScopeContext scope, object target, object expression, object scopeOptions)
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

        public IEnumerable<object> orderBy(TemplateScopeContext scope, object target, object filter) => orderBy(scope, target, filter, null);
        public IEnumerable<object> orderBy(TemplateScopeContext scope, object target, object filter, object scopeOptions) => 
            orderByInternal(nameof(orderBy), scope, target, filter, scopeOptions);

        public IEnumerable<object> orderByDescending(TemplateScopeContext scope, object target, object filter) => orderByDescending(scope, target, filter, null);
        public IEnumerable<object> orderByDescending(TemplateScopeContext scope, object target, object filter, object scopeOptions) =>
            orderByInternal(nameof(orderByDescending), scope, target, filter, scopeOptions);

        public IEnumerable<object> thenBy(TemplateScopeContext scope, object target, object filter) => thenBy(scope, target, filter, null);
        public IEnumerable<object> thenBy(TemplateScopeContext scope, object target, object filter, object scopeOptions) => 
            thenByInternal(nameof(thenBy), scope, target, filter, scopeOptions);

        public IEnumerable<object> thenByDescending(TemplateScopeContext scope, object target, object filter) => thenByDescending(scope, target, filter, null);
        public IEnumerable<object> thenByDescending(TemplateScopeContext scope, object target, object filter, object scopeOptions) =>
            thenByInternal(nameof(thenByDescending), scope, target, filter, scopeOptions);

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
            public bool Equals(object x, object y) => comparer.Equals(x, y);
            public int GetHashCode(object obj) => comparer.GetHashCode(obj);
        }
        class EqualityComparerWrapper<T> : IEqualityComparer<object>
        {
            private readonly IEqualityComparer<T> comparer;
            public EqualityComparerWrapper(IEqualityComparer<T> comparer) => this.comparer = comparer;
            public bool Equals(object x, object y) => comparer.Equals((T)x, (T)y);
            public int GetHashCode(object obj) => comparer.GetHashCode((T)obj);
        }

        private static IComparer<object> GetComparer(string filterName, TemplateScopeContext scope, Dictionary<string, object> scopedParams)
        {
            var comparer = (IComparer<object>) Comparer<object>.Default;
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
                : items.ThenBy(item =>scope.AddItemToScope(itemBinding, item, i++).Evaluate(value, binding), comparer);

            return sorted;
        }
        
        public IEnumerable<IGrouping<object, object>> groupBy(TemplateScopeContext scope, IEnumerable<object> items, object expression) => groupBy(scope, items, expression, null);
        public IEnumerable<IGrouping<object, object>> groupBy(TemplateScopeContext scope, IEnumerable<object> items, object expression, object scopeOptions) 
        {
            var literal = scope.AssertExpression(nameof(groupBy), expression);
            var scopedParams = scope.GetParamsWithItemBinding(nameof(groupBy), scopeOptions, out string itemBinding);

            literal.ToStringSegment().ParseNextToken(out object value, out JsBinding binding);

            var comparer = (IEqualityComparer<object>) EqualityComparer<object>.Default;
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
        
        public object map(TemplateScopeContext scope, object items, object expression) => map(scope, items, expression, null);
        public object map(TemplateScopeContext scope, object target, object expression, object scopeOptions) 
        {
            var literal = scope.AssertExpression(nameof(groupBy), expression);
            var scopedParams = scope.GetParamsWithItemBinding(nameof(map), scopeOptions, out string itemBinding);

            literal.ToStringSegment().ParseNextToken(out object value, out JsBinding binding);

            if (target is IEnumerable items && !(target is IDictionary) && !(target is string))
            {
                var i = 0;
                return items.Map(item => scope.AddItemToScope(itemBinding, item, i++).Evaluate(value, binding));
            }
            
            var result = scope.AddItemToScope(itemBinding, target).Evaluate(value, binding);
            return result;
        }
        
        public Task select(TemplateScopeContext scope, object target, object selectTemplate) => select(scope, target, selectTemplate, null);
        public async Task select(TemplateScopeContext scope, object target, object selectTemplate, object scopeOptions) 
        {
            var scopedParams = scope.GetParamsWithItemBinding(nameof(select), scopeOptions, out string itemBinding);
            var template = JsonTypeSerializer.Unescape(selectTemplate.ToString());
            var itemScope = scope.CreateScopedContext(template, scopedParams);

            if (target is IEnumerable objs && !(target is IDictionary) && !(target is string))
            {
                var i = 0;
                foreach (var item in objs)
                {
                    itemScope.AddItemToScope(itemBinding, item, i++);
                    await itemScope.WritePageAsync();
                }
            }
            else
            {
                itemScope.AddItemToScope(itemBinding, target);
                await itemScope.WritePageAsync();
            }
        }

        public Task selectPartial(TemplateScopeContext scope, object target, string pageName) => selectPartial(scope, target, pageName, null); 
        public async Task selectPartial(TemplateScopeContext scope, object target, string pageName, object scopedParams) 
        {
            var page = await scope.Context.GetPage(pageName).Init();
            var pageParams = scope.GetParamsWithItemBinding(nameof(selectPartial), page, scopedParams, out string itemBinding);

            if (target is IEnumerable objs && !(target is IDictionary) && !(target is string))
            {
                
                var i = 0;
                foreach (var item in objs)
                {
                    scope.AddItemToScope(itemBinding, item, i++);
                    await scope.WritePageAsync(page, pageParams);
                }
            }
            else if (target != null)
            {
                scope.AddItemToScope(itemBinding, target);
                await scope.WritePageAsync(page, pageParams);
            }
        }
        
        private async Task serialize(TemplateScopeContext scope, object items, string jsconfig, Func<object, string> fn)
        {
            var defaultJsConfig = Context.Args[TemplateConstants.DefaultJsConfig] as string;
            jsconfig  = jsconfig != null && !string.IsNullOrEmpty(defaultJsConfig)
                ? defaultJsConfig + "," + jsconfig
                : defaultJsConfig;
            
            if (jsconfig != null)
            {
                using (JsConfig.CreateScope(jsconfig))
                {
                    await scope.OutputStream.WriteAsync(fn(items));
                    return;
                }
            }
            await scope.OutputStream.WriteAsync(items.ToJson());
        }
        
        private IRawString serialize(object target, string jsconfig, Func<object, string> fn)
        {
            var defaultJsConfig = Context.Args[TemplateConstants.DefaultJsConfig] as string;
            jsconfig  = jsconfig != null && !string.IsNullOrEmpty(defaultJsConfig)
                ? defaultJsConfig + "," + jsconfig
                : defaultJsConfig;

            if (jsconfig == null) 
                return fn(target.AssertNoCircularDeps()).ToRawString();
           
            using (JsConfig.CreateScope(jsconfig))
            {
                return fn(target).ToRawString();
            }
        }
        
        //Filters
        public IRawString json(object value) => serialize(value, null, x => x.ToJson() ?? "null");
        public IRawString json(object value, string jsconfig) => serialize(value, jsconfig, x => x.ToJson() ?? "null");
        public IRawString jsv(object value) => serialize(value, null, x => x.ToJsv() ?? "");
        public IRawString jsv(object value, string jsconfig) => serialize(value, jsconfig, x => x.ToJsv() ?? "");
        public IRawString csv(object value) => (value.AssertNoCircularDeps().ToCsv() ?? "").ToRawString();
        public IRawString dump(object value) => serialize(value, null, x => x.Dump() ?? "");

        //Blocks
        public Task json(TemplateScopeContext scope, object items) => json(scope, items, null);
        public Task json(TemplateScopeContext scope, object items, string jsConfig) => serialize(scope, items, jsConfig, x => x.ToJson());

        public Task jsv(TemplateScopeContext scope, object items) => jsv(scope, items, null);
        public Task jsv(TemplateScopeContext scope, object items, string jsConfig) => serialize(scope, items, jsConfig, x => x.ToJsv());

        public Task dump(TemplateScopeContext scope, object items) => jsv(scope, items, null);
        public Task dump(TemplateScopeContext scope, object items, string jsConfig) => serialize(scope, items, jsConfig, x => x.Dump());

        public Task csv(TemplateScopeContext scope, object items) => scope.OutputStream.WriteAsync(items.ToCsv());
        public Task xml(TemplateScopeContext scope, object items) => scope.OutputStream.WriteAsync(items.ToXml());
    }
}