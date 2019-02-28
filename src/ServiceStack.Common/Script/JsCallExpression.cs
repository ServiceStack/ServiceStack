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
            if (Arguments.Length == 0)
            {
                var value = Callee.Evaluate(scope); 
                return value;
            }
            
            var result = scope.PageResult;

            var name = Name;

            var fnArgValues = EvaluateArgumentValues(scope, Arguments);
            var fnArgsLength = fnArgValues.Count;

            var invoker = result.GetFilterInvoker(name, fnArgsLength, out var filter);
            if (invoker != null)
            {
                var args = fnArgValues.ToArray();
                var value = result.InvokeFilter(invoker, filter, args, name);
                return value;
            }

            invoker = result.GetContextFilterInvoker(name, fnArgsLength + 1, out filter);
            if (invoker != null)
            {
                fnArgValues.Insert(0, scope);
                var args = fnArgValues.ToArray();
                var value = result.InvokeFilter(invoker, filter, args, name);
                return value;
            }

            throw new NotSupportedException(result.CreateMissingFilterErrorMessage(name.LeftPart('(')));
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