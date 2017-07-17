using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Text.Common;
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

        public IRawString json(object value)
        {
            if (value != null && TypeSerializer.HasCircularReferences(value))
                throw new NotSupportedException($"Cannot serialize type '{value.GetType().Name}' with cyclical dependencies");
            
            return (value.ToJson() ?? "null").ToRawString();
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

        public string format(object obj, string format) => string.Format(format, obj);

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

        public object join(IEnumerable<object> values) => join(values, ",");
        public object join(IEnumerable<object> values, string delimiter) => values.Map(x => x.ToString()).Join(delimiter);

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

        public List<object[]> zip(IEnumerable original, IEnumerable current)
        {
            var to = new List<object[]>();

            var currentArray = current.Cast<object>().ToArray();
            foreach (var a in original)
            {
                foreach (var b in current)
                {
                    to.Add(new[]{ a, b });
                }
            }

            return to;
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
                
                var itemScope = scope.CreateScopedContext(target.ToString(), scopedParams);
                foreach (var item in objs)
                {
                    itemScope.ScopedParams[itemBinding] = item;
                    await itemScope.WritePageAsync();
                }
            }
            else if (items != null)
            {
                throw new ArgumentException($"{nameof(forEach)} in '{scope.Page.VirtualPath}' requires an IEnumerable, but received a '{items.GetType().Name}' instead");
            }
        }

        public object where(TemplateScopeContext scope, object target, object filter) => where(scope, target, filter, null);
        public object where(TemplateScopeContext scope, object target, object filter, object scopeOptions)
        {
            var items = target.AssertEnumerable(nameof(where));

            if (!(filter is string literal)) 
                throw new NotSupportedException($"'{nameof(where)}' in '{scope.Page.VirtualPath}' requires a string Query Expression but received a '{filter?.GetType()?.Name}' instead");
            
            var scopedParams = scope.GetParamsWithItemBinding(nameof(select), scopeOptions, out string itemBinding);
            scopedParams.Each((key, val) => scope.ScopedParams[key] = val);

            var to = new List<object>();
            literal.ParseConditionExpression(out ConditionExpression expr);
            var i = 0;
            foreach (var item in items)
            {
                scope.ScopedParams[itemBinding] = item;
                scope.ScopedParams[TemplateConstants.Index] = i++;
                var result = expr.Evaluate(scope);
                if (result)
                {
                    to.Add(item);
                }
            }

            return to;
        }
        
        public Task select(TemplateScopeContext scope, object items, object target) => select(scope, items, target, null);
        public async Task select(TemplateScopeContext scope, object items, object target, object scopeOptions) 
        {
            var objs = items as IEnumerable;
            if (objs != null)
            {
                var scopedParams = scope.GetParamsWithItemBinding(nameof(select), scopeOptions, out string itemBinding);
                var template = JsonTypeSerializer.Unescape(target.ToString());
                var itemScope = scope.CreateScopedContext(template, scopedParams);
                
                var i = 0;
                foreach (var item in objs)
                {
                    itemScope.ScopedParams[itemBinding] = item;
                    itemScope.ScopedParams[TemplateConstants.Index] = i++;
                    await itemScope.WritePageAsync();
                }
            }
            else if (items != null)
            {
                throw new ArgumentException($"{nameof(select)} in '{scope.Page.VirtualPath}' requires an IEnumerable, but received a '{items.GetType().Name}' instead");
            }
        }

        public Task selectPartial(TemplateScopeContext scope, object items, string pageName) => selectPartial(scope, items, pageName, null); 
        public async Task selectPartial(TemplateScopeContext scope, object items, string pageName, object scopedParams) 
        {
            var objs = items as IEnumerable;
            if (objs != null)
            {
                var page = await scope.Context.GetPage(pageName).Init();
                var pageParams = scope.GetParamsWithItemBinding(nameof(selectPartial), page, scopedParams, out string itemBinding);
                
                var i = 0;
                foreach (var item in objs)
                {
                    pageParams[itemBinding] = item;
                    pageParams[TemplateConstants.Index] = i++;
                    await scope.WritePageAsync(page, pageParams);
                }
            }
            else if (items != null)
            {
                throw new ArgumentException($"{nameof(selectPartial)} in '{scope.Page.VirtualPath}' requires an IEnumerable, but received a '{items.GetType().Name}' instead");
            }
        }
    }
}