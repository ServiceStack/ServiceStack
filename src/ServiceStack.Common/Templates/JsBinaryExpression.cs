namespace ServiceStack.Templates
{
    public class JsBinaryExpression : JsExpression
    {
        public JsBinaryExpression(JsToken left, JsBinaryOperator operand, JsToken right)
        {
            Left = left;
            Operand = operand;
            Right = right;
        }

        public JsBinaryOperator Operand { get; set; }
        public JsToken Left { get; set; }
        public JsToken Right { get; set; }
        public override string ToRawString() => "(" + JsonValue(Left) + Operand.Token + JsonValue(Right) + ")";

        protected bool Equals(JsBinaryExpression other) =>
            Equals(Operand, other.Operand) && Equals(Left, other.Left) && Equals(Right, other.Right);

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
                var hashCode = (Operand != null ? Operand.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Left != null ? Left.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Right != null ? Right.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override object Evaluate(TemplateScopeContext scope)
        {
            var lhs = Left.Evaluate(scope);
            var rhs = Right.Evaluate(scope);
            return Operand.Evaluate(lhs, rhs);
        }
    }
}