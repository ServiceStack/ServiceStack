using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Text.Json;

namespace ServiceStack.Templates
{
    // ReSharper disable InconsistentNaming
    
    public partial class TemplateDefaultFilters : TemplateFilter
    {
        public static TemplateDefaultFilters Instance = new TemplateDefaultFilters();

        // methods without arguments can be used in bindings, e.g. {{ now | dateFormat }}
        public DateTime now() => DateTime.Now;
        public DateTime utcNow() => DateTime.UtcNow;

        public DateTime addTicks(DateTime target, int count) => target.AddTicks(count);
        public DateTime addMilliseconds(DateTime target, int count) => target.AddMilliseconds(count);
        public DateTime addSeconds(DateTime target, int count) => target.AddSeconds(count);
        public DateTime addMinutes(DateTime target, int count) => target.AddMinutes(count);
        public DateTime addHours(DateTime target, int count) => target.AddHours(count);
        public DateTime addDays(DateTime target, int count) => target.AddDays(count);
        public DateTime addMonths(DateTime target, int count) => target.AddMonths(count);
        public DateTime addYears(DateTime target, int count) => target.AddYears(count);

        public List<object> itemsOf(int count, object target)
        {
            AssertWithinMaxQuota(count);
            var to = new List<object>();
            for (var i = 0; i < count; i++)
            {
                to.Add(target);
            }
            return to;
        }

        public object times(int count) => AssertWithinMaxQuota(count).Times().ToList();
        public object range(int count) => Enumerable.Range(0, AssertWithinMaxQuota(count));
        public object range(int start, int count) => Enumerable.Range(start, AssertWithinMaxQuota(count));

        public bool isEven(int value) => value % 2 == 0;
        public bool isOdd(int value) => !isEven(value);

        public static bool isTrue(object target) => target is bool b && b;

        public static bool isTruthy(object target) => !isFalsy(target);
        public static bool isFalsy(object target)
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

        [HandleUnknownValue] public object iif(object test, object ifTrue, object ifFalse) => isTrue(test) ? ifTrue : ifFalse;
        [HandleUnknownValue] public object when(object returnTarget, object test) => @if(returnTarget, test);     //alias

        [HandleUnknownValue] public object ifNot(object returnTarget, object test) => !isTrue(test) ? returnTarget : null;
        [HandleUnknownValue] public object unless(object returnTarget, object test) => ifNot(returnTarget, test); //alias

        [HandleUnknownValue] public object otherwise(object returnTaget, object elseReturn) => returnTaget ?? elseReturn;

        [HandleUnknownValue] public object ifFalsy(object returnTarget, object test) => isFalsy(test) ? returnTarget : null;
        [HandleUnknownValue] public object ifTruthy(object returnTarget, object test) => !isFalsy(test) ? returnTarget : null;
        [HandleUnknownValue] public object falsy(object test, object returnIfFalsy) => isFalsy(test) ? returnIfFalsy : null;
        [HandleUnknownValue] public object truthy(object test, object returnIfTruthy) => !isFalsy(test) ? returnIfTruthy : null;

        [HandleUnknownValue] public bool isNull(object test) => test == null || test == JsNull.Value;
        [HandleUnknownValue] public bool isNotNull(object test) => !isNull(test);
        [HandleUnknownValue] public bool exists(object test) => !isNull(test);

        [HandleUnknownValue] public bool isZero(double value) => value.Equals(0d);
        [HandleUnknownValue] public bool isPositive(double value) => value > 0;
        [HandleUnknownValue] public bool isNegative(double value) => value < 0;
        [HandleUnknownValue] public bool isNaN(double value) => double.IsNaN(value);
        [HandleUnknownValue] public bool isInfinity(double value) => double.IsInfinity(value);

        [HandleUnknownValue] public object ifExists(object target) => target;
        [HandleUnknownValue] public object ifExists(object returnTarget, object test) => test != null ? returnTarget : null;
        [HandleUnknownValue] public object ifNotExists(object returnTarget, object target) => target == null ? returnTarget : null;
        [HandleUnknownValue] public object ifNo(object returnTarget, object target) => target == null ? returnTarget : null;
        [HandleUnknownValue] public object ifNotEmpty(object target) => isEmpty(target) ? null : target;
        [HandleUnknownValue] public object ifNotEmpty(object returnTarget, object test) => isEmpty(test) ? null : returnTarget;
        [HandleUnknownValue] public object ifEmpty(object returnTarget, object test) => isEmpty(test) ? returnTarget : null;
        [HandleUnknownValue] public object ifTrue(object returnTarget, object test) => isTrue(test) ? returnTarget : null;
        [HandleUnknownValue] public object ifFalse(object returnTarget, object test) => !isTrue(test) ? returnTarget : null;

        [HandleUnknownValue]
        public bool isEmpty(object target)
        {
            if (isNull(target))
                return true;

            if (target is string s)
                return s == string.Empty;

            if (target is IEnumerable e)
                return !e.GetEnumerator().MoveNext();

            return false;
        }

        [HandleUnknownValue]
        public bool IsNullOrWhiteSpace(object target) => target == null || target is string s && string.IsNullOrWhiteSpace(s);
        
        [HandleUnknownValue] public StopExecution end() => StopExecution.Value;
        [HandleUnknownValue] public Task end(TemplateScopeContext scope, object ignore) => TypeConstants.EmptyTask;
        [HandleUnknownValue] public StopExecution end(object ignore) => StopExecution.Value;

        [HandleUnknownValue] public object endIfNull(object target) => isNull(target) ? StopExecution.Value : target;
        [HandleUnknownValue] public object endIfNull(object ignoreTarget, object target) => isNull(target) ? StopExecution.Value : target;
        [HandleUnknownValue] public object endIfNotNull(object target) => !isNull(target) ? (object) StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object endIfNotNull(object ignoreTarget, object target) => !isNull(target) ? (object) StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object endIfExists(object target) => !isNull(target) ? (object) StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object endIfExists(object ignoreTarget, object target) => !isNull(target) ? (object) StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object endIfEmpty(object target) => isEmpty(target) ? StopExecution.Value : target;
        [HandleUnknownValue] public object endIfEmpty(object ignoreTarget, object target) => isEmpty(target) ? StopExecution.Value : target;
        [HandleUnknownValue] public object endIfNotEmpty(object target) => !isEmpty(target) ? (object) StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object endIfNotEmpty(object ignoreTarget, object target) => !isEmpty(target) ? (object) StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object endIfFalsy(object target) => isFalsy(target) ? (object) StopExecution.Value : target;
        [HandleUnknownValue] public object endIfFalsy(object ignoreTarget, object target) => isFalsy(target) ? (object) StopExecution.Value : target;
        [HandleUnknownValue] public object endIfTruthy(object target) => !isFalsy(target) ? (object) StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object endIfTruthy(object ignoreTarget, object target) => !isFalsy(target) ? (object) StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object endIf(object test) => isTrue(test) ? (object)StopExecution.Value : IgnoreResult.Value;

        [HandleUnknownValue] public object endIf(object returnTarget, bool test) => test ? StopExecution.Value : returnTarget;
        [HandleUnknownValue] public object endIfAny(TemplateScopeContext scope, object target, object expression) => any(scope, target, expression) ? StopExecution.Value : target;
        [HandleUnknownValue] public object endIfAll(TemplateScopeContext scope, object target, object expression) => all(scope, target, expression) ? StopExecution.Value : target;
        [HandleUnknownValue] public object endWhere(TemplateScopeContext scope, object target, object expression) => endWhere(scope, target, expression, null);

        [HandleUnknownValue]
        public object endWhere(TemplateScopeContext scope, object target, object expression, object scopeOptions)
        {
            var literal = scope.AssertExpression(nameof(count), expression);
            var scopedParams = scope.GetParamsWithItemBinding(nameof(count), scopeOptions, out string itemBinding);

            var expr = literal.GetCachedJsExpression(scope);
            scope.AddItemToScope(itemBinding, target);
            var result = expr.EvaluateToBool(scope);

            return result
                ? StopExecution.Value
                : target;
        }
        
        [HandleUnknownValue] public object ifEnd(bool test) => test ? (object)StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object ifEnd(object ignoreTarget, bool test) => test ? (object)StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object ifNotEnd(bool test) => !test ? (object)StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object ifNotEnd(object ignoreTarget, bool test) => !test ? (object)StopExecution.Value : IgnoreResult.Value;
        
        [HandleUnknownValue] public object onlyIfNull(object target) => !isNull(target) ? (object) StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object onlyIfNull(object ignoreTarget, object target) => !isNull(target) ? (object) StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object onlyIfNotNull(object target) => isNull(target) ? StopExecution.Value : target;
        [HandleUnknownValue] public object onlyIfNotNull(object ignoreTarget, object target) => isNull(target) ? StopExecution.Value : target;
        [HandleUnknownValue] public object onlyIfExists(object target) => isNull(target) ? (object) StopExecution.Value : target;
        [HandleUnknownValue] public object onlyIfExists(object ignoreTarget, object target) => isNull(target) ? (object) StopExecution.Value : target;
        [HandleUnknownValue] public object onlyIfEmpty(object target) => !isEmpty(target) ? (object) StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object onlyIfEmpty(object ignoreTarget, object target) => !isEmpty(target) ? (object) StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object onlyIfNotEmpty(object target) => isEmpty(target) ? (object) StopExecution.Value : target;
        [HandleUnknownValue] public object onlyIfNotEmpty(object ignoreTarget, object target) => isEmpty(target) ? (object) StopExecution.Value : target;
        [HandleUnknownValue] public object onlyIfFalsy(object target) => !isFalsy(target) ? (object) StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object onlyIfFalsy(object ignoreTarget, object target) => !isFalsy(target) ? (object) StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object onlyIfTruthy(object target) => isFalsy(target) ? (object) StopExecution.Value : target;
        [HandleUnknownValue] public object onlyIfTruthy(object ignoreTarget, object target) => isFalsy(target) ? (object) StopExecution.Value : target;
        [HandleUnknownValue] public object onlyIf(object test) => !isTrue(test) ? (object)StopExecution.Value : IgnoreResult.Value;

        [HandleUnknownValue] public object onlyIf(object returnTarget, bool test) => !test ? StopExecution.Value : returnTarget;
        [HandleUnknownValue] public object onlyIfAny(TemplateScopeContext scope, object target, object expression) => !any(scope, target, expression) ? StopExecution.Value : target;
        [HandleUnknownValue] public object onlyIfAll(TemplateScopeContext scope, object target, object expression) => !all(scope, target, expression) ? StopExecution.Value : target;
        [HandleUnknownValue] public object onlyWhere(TemplateScopeContext scope, object target, object expression) => onlyWhere(scope, target, expression, null);

        [HandleUnknownValue]
        public object onlyWhere(TemplateScopeContext scope, object target, object expression, object scopeOptions)
        {
            var literal = scope.AssertExpression(nameof(count), expression);
            var scopedParams = scope.GetParamsWithItemBinding(nameof(count), scopeOptions, out string itemBinding);

            var expr = literal.GetCachedJsExpression(scope);
            scope.AddItemToScope(itemBinding, target);
            var result = expr.EvaluateToBool(scope);

            return result
                ? target
                : StopExecution.Value;
        }
        
        [HandleUnknownValue] public object ifOnly(bool test) => !test ? (object)StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object ifOnly(object ignoreTarget, bool test) => !test ? (object)StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object ifNotOnly(bool test) => test ? (object)StopExecution.Value : IgnoreResult.Value;
        [HandleUnknownValue] public object ifNotOnly(object ignoreTarget, bool test) => test ? (object)StopExecution.Value : IgnoreResult.Value;


        [HandleUnknownValue] public object ifDo(object test) => isTrue(test) ? (object)IgnoreResult.Value : StopExecution.Value;
        [HandleUnknownValue] public object ifDo(object ignoreTarget, object test) => isTrue(test) ? (object)IgnoreResult.Value : StopExecution.Value;
        [HandleUnknownValue] public object doIf(object test) => isTrue(test) ? (object)IgnoreResult.Value : StopExecution.Value;
        [HandleUnknownValue] public object doIf(object ignoreTarget, object test) => isTrue(test) ? (object)IgnoreResult.Value : StopExecution.Value;

        [HandleUnknownValue] public object ifUse(object test, object useValue) => isTrue(test) ? useValue : StopExecution.Value;
        [HandleUnknownValue] public object ifShow(object test, object useValue) => isTrue(test) ? useValue : StopExecution.Value;
        [HandleUnknownValue] public object ifShowRaw(object test, object useValue) => isTrue(test) ? (object) raw(useValue) : StopExecution.Value;

        [HandleUnknownValue] public object useIf(object useValue, object test) => isTrue(test) ? useValue : StopExecution.Value;
        [HandleUnknownValue] public object showIf(object useValue, object test) => isTrue(test) ? useValue : StopExecution.Value;
        [HandleUnknownValue] public object showIfExists(object useValue, object test) => !isNull(test) ? useValue : StopExecution.Value;

        public object use(object ignoreTarget, object useValue) => useValue;
        public object show(object ignoreTarget, object useValue) => useValue;
        public IRawString showRaw(object ignoreTarget, string content) => content.ToRawString();

        public object useFmt(object ignoreTarget, string format, object arg) => fmt(format, arg);
        public object useFmt(object ignoreTarget, string format, object arg1, object arg2) => fmt(format, arg1, arg2);
        public object useFmt(object ignoreTarget, string format, object arg1, object arg2, object arg3) => fmt(format, arg1, arg2, arg3);
        public object useFormat(object ignoreTarget, object arg, string fmt) => format(arg, fmt);

        public object showFmt(object ignoreTarget, string format, object arg) => fmt(format, arg);
        public object showFmt(object ignoreTarget, string format, object arg1, object arg2) => fmt(format, arg1, arg2);
        public object showFmt(object ignoreTarget, string format, object arg1, object arg2, object arg3) => fmt(format, arg1, arg2, arg3);
        public object showFormat(object ignoreTarget, object arg, string fmt) => format(arg, fmt);

        public IRawString showFmtRaw(object ignoreTarget, string format, object arg) => raw(fmt(format, arg));
        public IRawString showFmtRaw(object ignoreTarget, string format, object arg1, object arg2) => raw(fmt(format, arg1, arg2));
        public IRawString showFmtRaw(object ignoreTarget, string format, object arg1, object arg2, object arg3) => raw(fmt(format, arg1, arg2, arg3));

        [HandleUnknownValue] public bool isString(object target) => target is string;
        [HandleUnknownValue] public bool isInt(object target) => target is int;
        [HandleUnknownValue] public bool isLong(object target) => target is long;
        [HandleUnknownValue] public bool isInteger(object target) => target?.GetType()?.IsIntegerType() == true;
        [HandleUnknownValue] public bool isDouble(object target) => target is double;
        [HandleUnknownValue] public bool isFloat(object target) => target is float;
        [HandleUnknownValue] public bool isDecimal(object target) => target is decimal;
        [HandleUnknownValue] public bool isBool(object target) => target is bool;
        [HandleUnknownValue] public bool isList(object target) => target is IEnumerable && !(target is IDictionary) && !(target is string);
        [HandleUnknownValue] public bool isEnumerable(object target) => target is IEnumerable;
        [HandleUnknownValue] public bool isDictionary(object target) => target is IDictionary;
        [HandleUnknownValue] public bool isChar(object target) => target is char;
        [HandleUnknownValue] public bool isChars(object target) => target is char[];
        [HandleUnknownValue] public bool isByte(object target) => target is byte;
        [HandleUnknownValue] public bool isBytes(object target) => target is byte[];
        [HandleUnknownValue] public bool isObjectDictionary(object target) => target is IDictionary<string, object>;
        [HandleUnknownValue] public bool isStringDictionary(object target) => target is IDictionary<string, string>;

        [HandleUnknownValue] public bool isType(object target, string typeName) => typeName.EqualsIgnoreCase(target?.GetType()?.Name);
        [HandleUnknownValue] public bool isNumber(object target) => target?.GetType().IsNumericType() == true;
        [HandleUnknownValue] public bool isRealNumber(object target) => target?.GetType().IsRealNumberType() == true;
        [HandleUnknownValue] public bool isEnum(object target) => target?.GetType().IsEnum == true;
        [HandleUnknownValue] public bool isArray(object target) => target?.GetType().IsArray == true;
        [HandleUnknownValue] public bool isAnonObject(object target) => target?.GetType().IsAnonymousType() == true;
        [HandleUnknownValue] public bool isClass(object target) => target?.GetType().IsClass == true;
        [HandleUnknownValue] public bool isValueType(object target) => target?.GetType().IsValueType == true;
        [HandleUnknownValue] public bool isDto(object target) => target?.GetType().IsDto() == true;
        [HandleUnknownValue] public bool isTuple(object target) => target?.GetType().IsTuple() == true;
        [HandleUnknownValue] public bool isKeyValuePair(object target) => "KeyValuePair`2".Equals(target?.GetType().Name);

        [HandleUnknownValue] public int length(object target) => target is IEnumerable e ? e.Cast<object>().Count() : 0;

        [HandleUnknownValue] public bool hasMinCount(object target, int minCount) => target is IEnumerable e && e.Cast<object>().Count() >= minCount;
        [HandleUnknownValue] public bool hasMaxCount(object target, int maxCount) => target is IEnumerable e && e.Cast<object>().Count() <= maxCount;

        public bool OR(object lhs, object rhs) => isTrue(lhs) || isTrue(rhs);
        public bool AND(object lhs, object rhs) => isTrue(lhs) && isTrue(rhs);

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
        public bool not(bool target) => !target;
        public bool eq(object target, object other) => equals(target, other);
        public bool not(object target, object other) => notEquals(target, other);
        public bool gt(object target, object other) => greaterThan(target, other);
        public bool gte(object target, object other) => greaterThanEqual(target, other);
        public bool lt(object target, object other) => lessThan(target, other);
        public bool lte(object target, object other) => lessThanEqual(target, other);

        internal static bool compareTo(object target, object other, Func<int, bool> fn)
        {
            if (target == null || target == JsNull.Value)
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
        public IRawString pass(string target) => ("{{ " + target + " }}").ToRawString();

        public IEnumerable join(IEnumerable<object> values) => join(values, ",");
        public IEnumerable join(IEnumerable<object> values, string delimiter) => values.Map(x => x.AsString()).Join(delimiter);

        public IEnumerable<object> reverse(TemplateScopeContext scope, IEnumerable<object> original) => original.Reverse();

        public KeyValuePair<string, object> keyValuePair(string key, object value) => new KeyValuePair<string, object>(key, value);

        public IgnoreResult prependTo(TemplateScopeContext scope, string value, object argExpr) =>
            prependToArgs(scope, nameof(prependTo), value, argExpr, scope.ScopedParams);

        public IgnoreResult prependToGlobal(TemplateScopeContext scope, string value, object argExpr) =>
            prependToArgs(scope, nameof(prependToGlobal), value, argExpr, scope.PageResult.Args);

        private IgnoreResult prependToArgs(TemplateScopeContext scope, string filterName, string value, object argExpr, Dictionary<string, object> args)
        {
            if (value == null)
                return IgnoreResult.Value;

            var varName = GetVarNameFromStringOrArrowExpression(filterName, argExpr);

            if (args.TryGetValue(varName, out object oString))
            {
                if (oString is string s)
                {
                    args[varName] = value + s;
                }
            }
            else
            {
                args[varName] = value;
            }
            
            return IgnoreResult.Value;
        }

        public IgnoreResult appendTo(TemplateScopeContext scope, string value, object argExpr) =>
            appendToArgs(scope, nameof(appendTo), value, argExpr, scope.ScopedParams);

        public IgnoreResult appendToGlobal(TemplateScopeContext scope, string value, object argExpr) =>
            appendToArgs(scope, nameof(appendToGlobal), value, argExpr, scope.PageResult.Args);

        private IgnoreResult appendToArgs(TemplateScopeContext scope, string filterName, string value, object argExpr, Dictionary<string,object> args)
        {
            if (value == null)
                return IgnoreResult.Value;

            var varName = GetVarNameFromStringOrArrowExpression(filterName, argExpr);

            if (args.TryGetValue(varName, out object oString))
            {
                if (oString is string s)
                {
                    args[varName] = s + value;
                }
            }
            else
            {
                args[varName] = value;
            }
            
            return IgnoreResult.Value;
        }

        public IgnoreResult addToStart(TemplateScopeContext scope, object value, object argExpr) =>
            addToStartArgs(scope, nameof(addToStart), value, argExpr, scope.ScopedParams);

        public IgnoreResult addToStartGlobal(TemplateScopeContext scope, object value, object argExpr) =>
            addToStartArgs(scope, nameof(addToStartGlobal), value, argExpr, scope.PageResult.Args);

        private IgnoreResult addToStartArgs(TemplateScopeContext scope, string filterName, object value, object argExpr, Dictionary<string,object> args)
        {
            if (value == null)
                return IgnoreResult.Value;

            var varName = GetVarNameFromStringOrArrowExpression(filterName, argExpr);

            if (args.TryGetValue(varName, out object collection))
            {
                if (collection is IList l)
                {
                    l.Insert(0, value);
                }                
                else if (collection is IEnumerable e && !(collection is string))
                {
                    var to = new List<object> { value };
                    foreach (var item in e)
                    {
                        to.Add(item);
                    }
                    args[varName] = to;
                }
                else throw new NotSupportedException(nameof(addToStart) + " can only add to an IEnumerable not a " + collection.GetType().Name);
            }
            else
            {
                if (value is IEnumerable && !(value is string))
                    args[varName] = value;
                else
                    args[varName] = new List<object> { value };
            }
            
            return IgnoreResult.Value;
        }

        public IgnoreResult addTo(TemplateScopeContext scope, object value, object argExpr) =>
            addToArgs(scope, nameof(addTo), value, argExpr, scope.ScopedParams);

        public IgnoreResult addToGlobal(TemplateScopeContext scope, object value, object argExpr) =>
            addToArgs(scope, nameof(addToGlobal), value, argExpr, scope.PageResult.Args);

        private IgnoreResult addToArgs(TemplateScopeContext scope, string filterName, object value, object argExpr, Dictionary<string, object> args)
        {
            if (value == null)
                return IgnoreResult.Value;

            var varName = GetVarNameFromStringOrArrowExpression(filterName, argExpr);

            if (args.TryGetValue(varName, out object collection))
            {
                if (collection is IList l)
                {
                    if (value is IEnumerable e && !(value is string || value is IDictionary))
                    {
                        foreach (var item in e)
                        {
                            l.Add(item);
                        }
                    }
                    else
                    {
                        l.Add(value);
                    }
                }                
                else if (collection is IDictionary d)
                {
                    if (value is KeyValuePair<string, object> kvp)
                    {
                        d[kvp.Key] = kvp.Value;
                    }
                    else if (value is IEnumerable<KeyValuePair<string, object>> kvps)
                    {
                        foreach (var entry in kvps)
                        {
                            d[entry.Key] = entry.Value;
                        }
                    }
                    else if (value is IDictionary dValue)
                    {
                        var keys = dValue.Keys;
                        foreach (var key in keys)
                        {
                            d[key] = dValue[key];
                        }
                    }
                }
                else if (collection is IEnumerable e && !(collection is string))
                {
                    var to = new List<object>();
                    foreach (var item in e)
                    {
                        to.Add(item);
                    }
                    if (value is IEnumerable eValues && !(value is string))
                    {
                        foreach (var item in eValues)
                        {
                            to.Add(item);
                        }
                    }
                    else
                    {
                        to.Add(value);
                    }
                    args[varName] = to;
                }
                else throw new NotSupportedException(nameof(addTo) + " can only add to an IEnumerable not a " + collection.GetType().Name);
            }
            else
            {
                if (value is IEnumerable && !(value is string || value is IDictionary))
                    args[varName] = value;
                else
                    args[varName] = new List<object> { value };
            }
            
            return IgnoreResult.Value;
        }

        public object assign(TemplateScopeContext scope, string argExpr, object value) =>
            assignArgs(scope, argExpr, value, scope.ScopedParams);

        public object assignGlobal(TemplateScopeContext scope, string argExpr, object value) =>
            assignArgs(scope, argExpr, value, scope.PageResult.Args);

        private object assignArgs(TemplateScopeContext scope, string argExpr, object value, Dictionary<string,object> args) //from filter
        {
            var targetEndPos = argExpr.IndexOfAny(new[] { '.', '[' });
            if (targetEndPos == -1)
            {
                args[argExpr] = value;
            }
            else
            {
                var targetName = argExpr.Substring(0, targetEndPos);
                if (!args.TryGetValue(targetName, out object target))
                    throw new NotSupportedException($"Cannot assign to non-existing '{targetName}' in {argExpr}");

                scope.InvokeAssignExpression(argExpr, target, value);
            }

            return value;
        }

        public IgnoreResult assignTo(TemplateScopeContext scope, object value, object argExpr)
        {
            var varName = GetVarNameFromStringOrArrowExpression(nameof(assignTo), argExpr);
            
            scope.ScopedParams[varName] = value;
            return IgnoreResult.Value;
        }

        public IgnoreResult assignToGlobal(TemplateScopeContext scope, object value, object argExpr)
        {
            var varName = GetVarNameFromStringOrArrowExpression(nameof(assignToGlobal), argExpr);
            
            scope.PageResult.Args[varName] = value;
            return IgnoreResult.Value;
        }

        public Task assignTo(TemplateScopeContext scope, object argExpr) =>
            assignToArgs(scope, nameof(assignTo), argExpr, scope.ScopedParams);

        public Task assignToGlobal(TemplateScopeContext scope, object argExpr) =>
            assignToArgs(scope, nameof(assignToGlobal), argExpr, scope.PageResult.Args);

        private Task assignToArgs(TemplateScopeContext scope, string filterName, object argExpr, Dictionary<string, object> args) //from context filter
        {
            var varName = GetVarNameFromStringOrArrowExpression(nameof(assignToGlobal), argExpr);

            var ms = (MemoryStream)scope.OutputStream;
            var value = ms.ReadToEnd();
            scope.ScopedParams[varName] = value;
            ms.SetLength(0); //just capture output, don't write anything to the ResponseStream
            return TypeConstants.EmptyTask;
        }

        public static string GetVarNameFromStringOrArrowExpression(string filterName, object argExpr)
        {
            if (argExpr == null)
                throw new ArgumentNullException(filterName);
            
            if (argExpr is JsArrowFunctionExpression arrowExpr)
            {
                if (!(arrowExpr.Body is JsIdentifier identifier))
                    throw new NotSupportedException($"{filterName} expression must return an identifer");

                return identifier.Name;
            }

            if (argExpr is string varName)
                return varName;

            throw new NotSupportedException($"{filterName} requires a string or expression identifier but was instead '{argExpr.GetType().Name}'");
        }

        public Task buffer(TemplateScopeContext scope, object target)
        {
            var ms = (MemoryStream)scope.OutputStream;
            return TypeConstants.EmptyTask;
        }

        public Task partial(TemplateScopeContext scope, object target) => partial(scope, target, null);
        public async Task partial(TemplateScopeContext scope, object target, object scopedParams)
        {
            var pageName = target.ToString();
            var pageParams = scope.AssertOptions(nameof(partial), scopedParams);

            if (!scope.TryGetPage(pageName, out var page, out var codePage))
            {
                //Allow partials starting with '_{name}-partial' to be referenced without boilerplate
                if (pageName[0] != '_')
                {
                    if (!scope.TryGetPage('_' + pageName + "-partial", out page, out codePage))
                        throw new FileNotFoundException($"Partial was not found: '{pageName}'");            
                }
            }
            
            if (page != null)
                await page.Init();

            if (page is TemplatePartialPage) // make partial block args available in scope
            {
                foreach (var pageArg in page.Args)
                {
                    pageParams[pageArg.Key] = pageArg.Value;
                }
            }

            pageParams["it"] = pageParams;

            await scope.WritePageAsync(page, codePage, pageParams);
        }

        public Task forEach(TemplateScopeContext scope, object target, object items) => forEach(scope, target, items, null);
        public async Task forEach(TemplateScopeContext scope, object target, object items, object scopeOptions)
        {
            if (items is IEnumerable objs)
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

        public char fromCharCode(int charCode) => Convert.ToChar(charCode);
        public char toChar(object target) => target is string s && s.Length == 1 ? s[0] : target.ConvertTo<char>();
        public char[] toChars(object target) => target is string s
            ? s.ToCharArray()
            : target is IEnumerable<object> objects
                ? objects.Where(x => x != null).Select(x => x.ToString()[0]).ToArray()
                : target.ConvertTo<char[]>();

        public int toCharCode(object target) => toChar(target);

        public byte[] toUtf8Bytes(string target) => target.ToUtf8Bytes();
        public string fromUtf8Bytes(byte[] target) => target.FromUtf8Bytes();

        public byte toByte(object target) => target.ConvertTo<byte>();
        public int toInt(object target) => target.ConvertTo<int>();
        public long toLong(object target) => target.ConvertTo<long>();
        public float toFloat(object target) => target.ConvertTo<float>();
        public double toDouble(object target) => target.ConvertTo<double>();
        public decimal toDecimal(object target) => target.ConvertTo<decimal>();
        public bool toBool(object target) => target.ConvertTo<bool>();
        public DateTime toDateTime(object target) => target.ConvertTo<DateTime>();
        public DateTime date(int year, int month, int day) => new DateTime(year, month, day);
        public DateTime date(int year, int month, int day, int hour, int min, int secs) => new DateTime(year, month, day, hour, min, secs);
        public TimeSpan toTimeSpan(object target) => target.ConvertTo<TimeSpan>();
        public TimeSpan time(int hours, int mins, int secs) => new TimeSpan(0, hours, mins, secs);
        public TimeSpan time(int days, int hours, int mins, int secs) => new TimeSpan(days, hours, mins, secs);

        public List<string> toKeys(object target)
        {
            if (target == null)
                return null;
            
            if (target is IDictionary<string, object> objDictionary)
                return objDictionary.Keys.ToList();
            if (target is IDictionary dictionary)
                return dictionary.Keys.Map(x => x.ToString());

            if (target is IEnumerable<KeyValuePair<string, object>> kvps)
            {
                var to = new List<string>();
                foreach (var kvp in kvps)
                {
                    to.Add(kvp.Key);
                }
                return to;
            }
            if (target is IEnumerable<KeyValuePair<string, string>> stringKvps)
            {
                var to = new List<string>();
                foreach (var kvp in stringKvps)
                {
                    to.Add(kvp.Key);
                }
                return to;
            }
            throw new NotSupportedException(nameof(toKeys) + " expects an IDictionary or List of KeyValuePairs but received: " + target.GetType().Name);
        }

        public List<object> toValues(object target)
        {
            if (target == null)
                return null;
            
            if (target is IDictionary<string, object> objDictionary)
                return objDictionary.Values.ToList();
            if (target is IDictionary dictionary)
                return dictionary.Values.Map(x => x);

            if (target is IEnumerable<KeyValuePair<string, object>> kvps)
            {
                var to = new List<object>();
                foreach (var kvp in kvps)
                {
                    to.Add(kvp.Value);
                }
                return to;
            }
            if (target is IEnumerable<KeyValuePair<string, string>> stringKvps)
            {
                var to = new List<object>();
                foreach (var kvp in stringKvps)
                {
                    to.Add(kvp.Value);
                }
                return to;
            }
            throw new NotSupportedException(nameof(toValues) + " expects an IDictionary or List of KeyValuePairs but received: " + target.GetType().Name);
        }

        public Dictionary<string, object> toObjectDictionary(object target) => target.ToObjectDictionary();
        public Dictionary<string, string> toStringDictionary(IDictionary map)
        {
            if (isNull(map))
                return null;

            var to = new Dictionary<string, string>();
            foreach (var key in map.Keys)
            {
                var value = map[key];
                to[key.ToString()] = value?.ToString();
            }
            return to;
        }

        public int AssertWithinMaxQuota(int value)
        {
            var maxQuota = (int)Context.Args[TemplateConstants.MaxQuota];
            if (value > maxQuota)
                throw new NotSupportedException($"{value} exceeds Max Quota of {maxQuota}");

            return value;
        }

        public Dictionary<object, object> toDictionary(TemplateScopeContext scope, object target, object expression) => toDictionary(scope, target, expression, null);
        public Dictionary<object, object> toDictionary(TemplateScopeContext scope, object target, object expression, object scopeOptions)
        {
            var items = target.AssertEnumerable(nameof(toDictionary));
            var token = scope.AssertExpression(nameof(map), expression, scopeOptions, out var itemBinding);

            return items.ToDictionary(item => token.Evaluate(scope.AddItemToScope(itemBinding, item)));
        }

        public IRawString typeName(object target) => (target?.GetType().Name ?? "null").ToRawString();

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

        public object @do(TemplateScopeContext scope, object expression)
        {
            var token = scope.AssertExpression(nameof(@do), expression, scopeOptions:null, out var itemBinding);
            var result = token.Evaluate(scope);

            return IgnoreResult.Value;
        }

        [HandleUnknownValue]
        public Task @do(TemplateScopeContext scope, object target, object expression) => @do(scope, target, expression, null);
        [HandleUnknownValue]
        public Task @do(TemplateScopeContext scope, object target, object expression, object scopeOptions)
        {
            if (isNull(target) || target is bool b && !b)
                return TypeConstants.EmptyTask;

            var token = scope.AssertExpression(nameof(@do), expression, scopeOptions, out var itemBinding);

            if (target is IEnumerable objs && !(target is IDictionary) && !(target is string))
            {
                var items = target.AssertEnumerable(nameof(@do));

                var i = 0;
                var eagerItems = items.ToArray(); // assign on array expression can't be within enumerable 
                foreach (var item in eagerItems)
                {
                    scope.AddItemToScope(itemBinding, item, i++);
                    var result = token.Evaluate(scope);
                }
            }
            else
            {
                scope.AddItemToScope(itemBinding, target);
                var result = token.Evaluate(scope);
            }

            return TypeConstants.EmptyTask;
        }

        public object property(object target, string propertyName)
        {
            if (isNull(target))
                return null;

            var props = TypeProperties.Get(target.GetType());
            var fn = props.GetPublicGetter(propertyName);
            if (fn == null)
                throw new NotSupportedException($"There is no public Property '{propertyName}' on Type '{target.GetType().Name}'");

            var value = fn(target);
            return value;
        }

        public object field(object target, string fieldName)
        {
            if (isNull(target))
                return null;

            var props = TypeFields.Get(target.GetType());
            var fn = props.GetPublicGetter(fieldName);
            if (fn == null)
                throw new NotSupportedException($"There is no public Field '{fieldName}' on Type '{target.GetType().Name}'");

            var value = fn(target);
            return value;
        }
        
        public object map(TemplateScopeContext scope, object items, object expression) => map(scope, items, expression, null);
        public object map(TemplateScopeContext scope, object target, object expression, object scopeOptions)
        {
            var token = scope.AssertExpression(nameof(map), expression, scopeOptions, out var itemBinding);

            if (target is IEnumerable items && !(target is IDictionary) && !(target is string))
            {
                var i = 0;
                return items.Map(item => token.Evaluate(scope.AddItemToScope(itemBinding, item, i++)));
            }

            var result = token.Evaluate(scope.AddItemToScope(itemBinding, target));
            return result;
        }
        
        public object scopeVars(object target)
        {
            if (isNull(target))
                return null;

            if (target is IDictionary<string, object> g)
                return new ScopeVars(g);

            if (target is IDictionary d)
            {
                var to = new ScopeVars();
                foreach (var key in d.Keys)
                {
                    to[key.ToString()] = d[key];
                }
                return to;
            }

            if (target is IEnumerable<KeyValuePair<string, object>> kvps)
            {
                var to = new ScopeVars();
                foreach (var item in kvps)
                {
                    to[item.Key] = item.Value;
                }
                return to;
            }

            if (target is IEnumerable e)
            {
                var to = new List<object>();

                foreach (var item in e)
                {
                    var toItem = item is IDictionary
                        ? scopeVars(item)
                        : item;

                    to.Add(toItem);
                }

                return to;
            }

            throw new NotSupportedException($"'{nameof(scopeVars)}' expects a Dictionary but received a '{target.GetType().Name}'");
        }

        public object selectFields(object target, object names)
        {
            if (target == null || names == null)
                return null;
            
            if (target is string || target.GetType().IsValueType)
                throw new NotSupportedException(nameof(selectFields) + " requires an IEnumerable, IDictionary or POCO Target, received instead: " + target.GetType().Name);

            var namesList = names is IEnumerable eKeys
                ? eKeys.Map(x => x)
                : null;

            var stringKey = names as string;
            var stringKeys = namesList?.OfType<string>().ToList();
            if (stringKeys.IsEmpty())
                stringKeys = null;

            if (stringKey == null && stringKeys == null)
                throw new NotSupportedException(nameof(selectFields) + " requires a string or [string] or property names, received instead: " + names.GetType().Name);

            if (stringKey?.IndexOf(',') >= 0)
            {
                stringKeys = stringKey.Split(',').Map(x => x.Trim());
                stringKey = null;
            }
            
            var stringsSet = stringKeys != null
                ? new HashSet<string>(stringKeys, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string> { stringKey };

            var singleItem = target is IDictionary || !(target is IEnumerable);
            if (singleItem)
            {
                var objDictionary = target.ToObjectDictionary();

                var to = new Dictionary<string, object>();
                foreach (var key in objDictionary.Keys)
                {
                    if (stringsSet.Contains(key))
                        to[key] = objDictionary[key];
                }

                return to;
            }
            else 
            {
                var to = new List<Dictionary<string,object>>();
                var e = (IEnumerable) target;
                foreach (var item in e)
                {
                    var objDictionary = item.ToObjectDictionary();

                    var row = new Dictionary<string, object>();
                    foreach (var key in objDictionary.Keys)
                    {
                        if (stringsSet.Contains(key))
                            row[key] = objDictionary[key];
                    }
                    to.Add(row);
                }
                return to;
            }
        }

        [HandleUnknownValue]
        public Task select(TemplateScopeContext scope, object target, object selectTemplate) => select(scope, target, selectTemplate, null);
        [HandleUnknownValue]
        public async Task select(TemplateScopeContext scope, object target, object selectTemplate, object scopeOptions)
        {
            if (isNull(target))
                return;

            var scopedParams = scope.GetParamsWithItemBinding(nameof(select), scopeOptions, out string itemBinding);
            var template = JsonTypeSerializer.Unescape(selectTemplate.ToString(), removeQuotes:false);
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

        [HandleUnknownValue]
        public Task selectPartial(TemplateScopeContext scope, object target, string pageName) => selectPartial(scope, target, pageName, null);
        [HandleUnknownValue]
        public async Task selectPartial(TemplateScopeContext scope, object target, string pageName, object scopedParams)
        {
            if (isNull(target))
                return;

            if (!scope.TryGetPage(pageName, out var page, out var codePage))
            {
                //Allow partials starting with '_{name}-partial' to be referenced without boilerplate
                if (pageName[0] != '_')
                {
                    if (!scope.TryGetPage('_' + pageName + "-partial", out page, out codePage))
                        throw new FileNotFoundException($"Partial was not found: '{pageName}'");            
                }
            }

            if (page != null)
                await page.Init();

            var pageParams = scope.GetParamsWithItemBinding(nameof(selectPartial), page, scopedParams, out string itemBinding);

            if (page is TemplatePartialPage) // make partial block args available in scope
            {
                foreach (var pageArg in page.Args)
                {
                    pageParams[pageArg.Key] = pageArg.Value;
                }
            }

            if (target is IEnumerable objs && !(target is IDictionary) && !(target is string))
            {

                var i = 0;
                foreach (var item in objs)
                {
                    scope.AddItemToScope(itemBinding, item, i++);
                    await scope.WritePageAsync(page, codePage, pageParams);
                }
            }
            else
            {
                scope.AddItemToScope(itemBinding, target);
                await scope.WritePageAsync(page, codePage, pageParams);
            }
        }
        
        public object removeKeyFromDictionary(IDictionary dictionary, object keyToRemove)
        {
            var removeKeys = keyToRemove is IEnumerable e
                ? e.Map(x => x)
                : null;
            
            foreach (var key in dictionary.Keys)
            {
                if (removeKeys != null)
                {
                    foreach (var removeKey in removeKeys)
                    {
                        if (Equals(key, removeKey))
                            dictionary.Remove(key);
                    }
                }
                else if (Equals(key, keyToRemove))
                {
                    dictionary.Remove(key);
                }
            }
            return dictionary;
        }
        
        public object remove(object target, object keysToRemove)
        {
            var removeKeys = keysToRemove is IEnumerable eKeys
                ? eKeys.Map(x => x)
                : null;

            var stringKey = keysToRemove as string;
            var stringKeys = removeKeys?.OfType<string>().ToArray();
            if (stringKeys.IsEmpty())
                stringKeys = null;

            if (target is IDictionary d)
                return removeKeyFromDictionary(d, removeKeys);
            
            if (target is IEnumerable e)
            {
                object first = null;
                foreach (var item in e)
                {
                    if (item == null) 
                        continue;
                    
                    first = item;
                    break;
                }
                if (first == null)
                    return target;

                var itemType = first.GetType();
                var props = TypeProperties.Get(itemType);
                
                if (!(first is IDictionary))
                    throw new NotSupportedException(nameof(remove) + " removes keys from a IDictionary or [IDictionary]");
                
                foreach (var item in e)
                {
                    if (item == null)
                        continue;

                    if (item is IDictionary ed)
                    {
                        removeKeyFromDictionary(ed, removeKeys);
                    }
                }
            }
            else throw new NotSupportedException(nameof(remove) + " removes keys from a IDictionary or [IDictionary]");
            
            return target;
        }
        
        public object withoutNullValues(object target)
        {
            if (target is IDictionary<string, object> objDictionary)
            {
                var keys = objDictionary.Keys.ToList();
                var to = new Dictionary<string, object>();
                foreach (var key in keys)
                {
                    var value = objDictionary[key];
                    if (!isNull(value))
                    {
                        to[key] = value;
                    }
                }
                return to;
            }
            if (target is IEnumerable list)
            {
                var to = new List<object>();
                foreach (var item in list)
                {
                    if (!isNull(item))
                        to.Add(item);
                }
                return to;
            }
            return target;
        }
        
        public object withoutEmptyValues(object target)
        {
            if (target is IDictionary<string, object> objDictionary)
            {
                var keys = objDictionary.Keys.ToList();
                var to = new Dictionary<string, object>();
                foreach (var key in keys)
                {
                    var value = objDictionary[key];
                    if (!isEmpty(value))
                    {
                        to[key] = value;
                    }
                }
                return to;
            }
            if (target is IEnumerable list)
            {
                var to = new List<object>();
                foreach (var item in list)
                {
                    if (!isEmpty(item))
                        to.Add(item);
                }
                return to;
            }
            return target;
        }

        public object withKeys(IDictionary<string, object> target, object keys)
        {
            if (keys == null)
                return target;
            
            var strKeys = keys is string s
                ? new List<string>{ s }
                : keys is IEnumerable e
                    ? e.Map(x => x.ToString())
                    : throw new NotSupportedException($"{nameof(withoutKeys)} expects a collection of key names but received ${keys.GetType().Name}");

            var to = new Dictionary<string, object>();
            foreach (var entry in target)
            {
                if (!strKeys.Contains(entry.Key))
                    continue;

                to[entry.Key] = entry.Value;
            }
            return to;
        }

        public object withoutKeys(IDictionary<string, object> target, object keys)
        {
            if (keys == null)
                return target;
            
            var strKeys = keys is string s
                ? new List<string>{ s }
                : keys is IEnumerable e
                    ? e.Map(x => x.ToString())
                    : throw new NotSupportedException($"{nameof(withoutKeys)} expects a collection of key names but received ${keys.GetType().Name}");

            var to = new Dictionary<string, object>();
            foreach (var entry in target)
            {
                if (strKeys.Contains(entry.Key))
                    continue;

                to[entry.Key] = entry.Value;
            }
            return to;
        }

        public object merge(IDictionary<string, object> target, object sources)
        {
            var srcArray = sources is IDictionary<string, object> d
                ? new object[] {d}
                : sources is List<IDictionary<string, object>> ld
                    ? ld.ToArray()
                    : sources is List<object> lo
                        ? lo.ToArray()
                        : sources is object[] la
                            ? la
                            : throw new NotSupportedException(
                                $"{nameof(merge)} cannot merge objects of type ${sources.GetType().Name}");

            return target.MergeIntoObjectDictionary(srcArray);
        }

        public string dirPath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || filePath[filePath.Length - 1] == '/')
                return null;

            var lastDirPos = filePath.LastIndexOf('/');
            return lastDirPos >= 0
                ? filePath.Substring(0, lastDirPos)
                : null;
        }

        public string resolveAsset(TemplateScopeContext scope, string virtualPath)
        {
            if (string.IsNullOrEmpty(virtualPath))
                return string.Empty;

            if (!scope.Context.Args.TryGetValue(TemplateConstants.AssetsBase, out object assetsBase))
                return virtualPath;

            return virtualPath[0] == '/'
                ? assetsBase.ToString().CombineWith(virtualPath).ResolvePaths()
                : assetsBase.ToString().CombineWith(dirPath(scope.Page.VirtualPath), virtualPath).ResolvePaths();
        }

        public Task<object> evalTemplate(TemplateScopeContext scope, string source) => evalTemplate(scope, source, null);
        public async Task<object> evalTemplate(TemplateScopeContext scope, string source, Dictionary<string, object> args)
        {
            var context = scope.CreateNewContext(args);
            
            using (var ms = MemoryStreamFactory.GetStream())
            {
                var pageResult = new PageResult(context.OneTimePage(source));
                if (args != null)
                    pageResult.Args = args;
                
                await pageResult.WriteToAsync(ms);

                ms.Position = 0;
                var result = ms.ReadToEnd();

                return result;
            }
        }

   }

    public partial class TemplateDefaultFilters //Methods named after common keywords breaks intelli-sense when trying to use them        
    {
        [HandleUnknownValue] public object @if(object test) => test is bool b && b ? (object) IgnoreResult.Value : StopExecution.Value;
        [HandleUnknownValue] public object @if(object returnTarget, object test) => test is bool b && b ? returnTarget : null;
        [HandleUnknownValue] public object @default(object returnTaget, object elseReturn) => returnTaget ?? elseReturn;

        public object @throw(TemplateScopeContext scope, string message) => new Exception(message).InStopFilter(scope, null);
        public object @throw(TemplateScopeContext scope, string message, object options) => new Exception(message).InStopFilter(scope, options);
        
        [HandleUnknownValue] public StopExecution @return(TemplateScopeContext scope) => @return(scope, null, null);
        [HandleUnknownValue] public StopExecution @return(TemplateScopeContext scope, object returnValue) => @return(scope, returnValue, null);
        [HandleUnknownValue] public StopExecution @return(TemplateScopeContext scope, object returnValue, object returnArgs)
        {
            scope.PageResult.Args[TemplateConstants.Return] = returnValue;
            scope.PageResult.Args[TemplateConstants.ReturnArgs] = returnArgs;
            scope.PageResult.HaltExecution = true;
            return StopExecution.Value;
        }
    }
}