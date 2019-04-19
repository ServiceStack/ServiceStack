using System;
using System.Collections;
using System.Collections.Generic;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    public class JsCallExpression : JsExpression
    {
        public JsToken Callee { get; }
        public JsToken[] Arguments { get; }

        public JsCallExpression(JsToken callee, params JsToken[] arguments)
        {
            Callee = callee;
            Arguments = arguments;
        }

        private string nameString;
        public string Name => nameString ?? (nameString = Callee is JsIdentifier identifier ? identifier.Name : null);
        
        public override object Evaluate(ScriptScopeContext scope)
        {
            string ResolveMethodName(JsMemberExpression expr)
            {
                if (!expr.Computed) 
                    return JsObjectExpression.GetKey(expr.Property);
                
                var propValue = expr.Property.Evaluate(scope);
                var s = propValue as string;
                if (s == null)
                    throw new NotSupportedException($"Expected string method name but was '{propValue?.GetType().Name ?? "null"}'");
                return s;
            }

            MethodInvoker invoker;
            ScriptMethods filter;
            object value;
            string name;
            var result = scope.PageResult;

            if (Arguments.Length == 0)
            {
                if (Callee is JsMemberExpression expr)
                {
                    name = ResolveMethodName(expr);
                    
                    invoker = result.GetFilterInvoker(name, 1, out filter);
                    if (invoker != null)
                    {
                        var targetValue = expr.Object.Evaluate(scope);                        
                        value = result.InvokeFilter(invoker, filter, new[]{ targetValue }, name);
                        return value;
                    }

                    invoker = result.GetContextFilterInvoker(name, 2, out filter);
                    if (invoker != null)
                    {
                        var targetValue = expr.Object.Evaluate(scope);
                        value = result.InvokeFilter(invoker, filter, new[]{ scope, targetValue }, name);
                        return value;
                    }
                }
                
                value = Callee.Evaluate(scope); 
                return value;
            }
            else
            {
                var fnArgValuesCount = Arguments.Length;
                foreach (var arg in Arguments)
                {                    
                    if (arg is JsSpreadElement) // ...[1,2] Arguments.Length = 1 / fnArgValues.Length = 2
                    {
                        var expandedArgs = EvaluateArgumentValues(scope, Arguments);
                        fnArgValuesCount = expandedArgs.Count;
                        break;
                    }
                }

                if (Callee is JsMemberExpression expr)
                {
                    name = ResolveMethodName(expr);
                    
                    invoker = result.GetFilterInvoker(name, fnArgValuesCount + 1, out filter);
                    if (invoker != null)
                    {
                        var targetValue = expr.Object.Evaluate(scope);
                        var fnArgValues = EvaluateArgumentValues(scope, Arguments);   
                        fnArgValues.Insert(0, targetValue);
                        
                        value = result.InvokeFilter(invoker, filter, fnArgValues.ToArray(), name);
                        return value;
                    }
                    
                    invoker = result.GetContextFilterInvoker(name, fnArgValuesCount + 2, out filter);
                    if (invoker != null)
                    {
                        var targetValue = expr.Object.Evaluate(scope);
                        var fnArgValues = EvaluateArgumentValues(scope, Arguments); 
                        fnArgValues.InsertRange(0, new[]{ scope, targetValue });
                        value = result.InvokeFilter(invoker, filter, fnArgValues.ToArray(), name);
                        return value;
                    }
                }
                else
                {
                    name = Name;

                    invoker = result.GetFilterInvoker(name, fnArgValuesCount, out filter);
                    if (invoker != null)
                    {
                        var fnArgValues = EvaluateArgumentValues(scope, Arguments); 
                        value = result.InvokeFilter(invoker, filter, fnArgValues.ToArray(), name);
                        return value;
                    }

                    invoker = result.GetContextFilterInvoker(name, fnArgValuesCount + 1, out filter);
                    if (invoker != null)
                    {
                        var fnArgValues = EvaluateArgumentValues(scope, Arguments);
                        fnArgValues.Insert(0, scope);
                        value = result.InvokeFilter(invoker, filter, fnArgValues.ToArray(), name);
                        return value;
                    }
                }

                throw new NotSupportedException(result.CreateMissingFilterErrorMessage(name.LeftPart('(')));
            }
        }

        public static List<object> EvaluateArgumentValues(ScriptScopeContext scope, JsToken[] args)
        {
            var fnArgValues = new List<object>(args.Length + 2); //max size of args without spread args
            foreach (var arg in args)
            {
                if (arg is JsSpreadElement spread)
                {
                    if (!(spread.Argument.Evaluate(scope) is IEnumerable spreadValues))
                        continue;
                    foreach (var argValue in spreadValues)
                    {
                        fnArgValues.Add(argValue);
                    }
                }
                else
                {
                    fnArgValues.Add(arg.Evaluate(scope));
                }
            }

            return fnArgValues;
        }

        public override string ToString() => ToRawString();

        public override string ToRawString()
        {
            var sb = StringBuilderCacheAlt.Allocate();
            foreach (var arg in Arguments)
            {
                if (sb.Length > 0)
                    sb.Append(',');
                sb.Append(arg.ToRawString());
            }
            return $"{Callee.ToRawString()}({StringBuilderCacheAlt.ReturnAndFree(sb)})";
        }

        public string GetDisplayName() => (Name ?? "").Replace('′', '"');

        protected bool Equals(JsCallExpression other)
        {
            return Equals(Callee, other.Callee) &&
                   Arguments.EquivalentTo(other.Arguments);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsCallExpression) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Callee != null ? Callee.GetHashCode() : 0) * 397) ^
                       (Arguments != null ? Arguments.GetHashCode() : 0);
            }
        }

        public override Dictionary<string, object> ToJsAst()
        {
            var arguments = new List<object>();
            var to = new Dictionary<string, object> {
                ["type"] = ToJsAstType(),
                ["callee"] = Callee.ToJsAst(),
                ["arguments"] = arguments,
            };

            foreach (var argument in Arguments)
            {
                arguments.Add(argument.ToJsAst());
            }

            return to;
        }
    }
}