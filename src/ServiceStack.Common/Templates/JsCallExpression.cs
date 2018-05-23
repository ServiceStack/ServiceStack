using System;
using System.Collections.Generic;
using ServiceStack.Text;

#if NETSTANDARD2_0
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public class JsCallExpression : JsToken
    {
        public JsToken Callee { get; }
        public JsToken[] Arguments { get; }

        public JsCallExpression(JsToken callee, params JsToken[] arguments)
        {
            Callee = callee;
            Arguments = arguments;
        }

        private string nameString;
        public string Name => nameString ?? (nameString = Callee is JsIdentifier identifier ? identifier.NameString : null);

        public override object Evaluate(TemplateScopeContext scope)
        {
            if (Arguments.Length == 0)
            {
                var value = Callee.Evaluate(scope); 
                return value;
            }
            
            var result = scope.PageResult;

            var name = Name;

            var invoker = result.GetFilterInvoker(name, Arguments.Length, out var filter);
            if (invoker != null)
            {
                var args = new object[Arguments.Length];
                for (var i = 0; i < Arguments.Length; i++)
                {
                    var arg = Arguments[i];
                    var varValue = arg.Evaluate(scope);
                    args[i] = varValue;
                }

                var value = result.InvokeFilter(invoker, filter, args, name);
                return value;
            }

            invoker = result.GetContextFilterInvoker(name, Arguments.Length + 1, out filter);
            if (invoker != null)
            {
                var args = new object[Arguments.Length + 1];
                args[0] = scope;
                for (var i = 0; i < Arguments.Length; i++)
                {
                    var arg = Arguments[i];
                    var varValue = arg.Evaluate(scope);
                    args[i + 1] = varValue;
                }

                var value = result.InvokeFilter(invoker, filter, args, name);
                return value;
            }

            throw new NotSupportedException(result.CreateMissingFilterErrorMessage(name.LeftPart('(')));
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
            return $"{Name}({StringBuilderCacheAlt.ReturnAndFree(sb)})";
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
    }
}