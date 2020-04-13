using System.Collections.Generic;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    public class JsConditionalExpression : JsExpression
    {
        public JsToken Test { get; }

        public JsToken Consequent { get; }

        public JsToken Alternate { get; }

        public JsConditionalExpression(JsToken test, JsToken consequent, JsToken alternate)
        {
            Test = test ?? throw new SyntaxErrorException($"Test Expression missing in Conditional Expression");
            Consequent = consequent ?? throw new SyntaxErrorException($"Consequent Expression missing in Conditional Expression");
            Alternate = alternate ?? throw new SyntaxErrorException($"Alternate Expression missing in Conditional Expression");
        }

        public override string ToRawString()
        {
            var sb = StringBuilderCache.Allocate();
            sb.Append(Test.ToRawString());
            sb.Append(" ? ");
            sb.Append(Consequent.ToRawString());
            sb.Append(" : ");
            sb.Append(Alternate.ToRawString());
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public override Dictionary<string, object> ToJsAst()
        {
            var to = new Dictionary<string, object>
            {
                ["type"] = ToJsAstType(),
                ["test"] = Test.ToJsAst(),
                ["consequent"] = Consequent.ToJsAst(),
                ["alternate"] = Alternate.ToJsAst(),
            };
            return to;
        }

        public override object Evaluate(ScriptScopeContext scope)
        {
            var test = Test.EvaluateToBool(scope);
            var value = test
                ? Consequent.Evaluate(scope)
                : Alternate.Evaluate(scope);
            return value;
        }

        protected bool Equals(JsConditionalExpression other)
        {
            return Equals(Test, other.Test) &&
                   Equals(Consequent, other.Consequent) &&
                   Equals(Alternate, other.Alternate);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsConditionalExpression) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Test != null ? Test.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Consequent != null ? Consequent.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Alternate != null ? Alternate.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}