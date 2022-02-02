using System.Collections.Generic;

namespace ServiceStack.Script
{
    public class JsBinaryExpression : JsExpression
    {
        public JsBinaryExpression(JsToken left, JsBinaryOperator @operator, JsToken right)
        {
            Left = left ?? throw new SyntaxErrorException($"Left Expression missing in Binary Expression");
            Operator = @operator ?? throw new SyntaxErrorException($"Operator missing in Binary Expression");
            Right = right ?? throw new SyntaxErrorException($"Right Expression missing in Binary Expression");
        }

        public JsBinaryOperator Operator { get; set; }
        public JsToken Left { get; set; }
        public JsToken Right { get; set; }
        public override string ToRawString() => "(" + JsonValue(Left) + Operator.Token + JsonValue(Right) + ")";

        protected bool Equals(JsBinaryExpression other) =>
            Equals(Operator, other.Operator) && Equals(Left, other.Left) && Equals(Right, other.Right);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsBinaryExpression) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Operator != null ? Operator.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Left != null ? Left.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Right != null ? Right.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override object Evaluate(ScriptScopeContext scope)
        {
            var lhs = Left.Evaluate(scope);
            var rhs = Right.Evaluate(scope);
            return Operator.Evaluate(lhs, rhs);
        }

        public override Dictionary<string, object> ToJsAst()
        {
            var to = new Dictionary<string, object>
            {
                ["type"] = ToJsAstType(),
                ["operator"] = Operator.Token,
                ["left"] = Left.ToJsAst(),
                ["right"] = Right.ToJsAst(),
            };
            return to;
        }
    }
}