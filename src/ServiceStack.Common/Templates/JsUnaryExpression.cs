namespace ServiceStack.Templates
{
    public class JsUnaryExpression : JsExpression
    {
        public JsUnaryOperator Operator { get; }
        public JsToken Argument { get; }
        public override string ToRawString() => Operator.Token + JsonValue(Argument);

        public JsUnaryExpression(JsUnaryOperator @operator, JsToken argument)
        {
            Operator = @operator;
            Argument = argument;
        }

        protected bool Equals(JsUnaryExpression other) => Equals(Operator, other.Operator) && Equals(Argument, other.Argument);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsUnaryExpression) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Operator != null ? Operator.GetHashCode() : 0) * 397) ^ (Argument != null ? Argument.GetHashCode() : 0);
            }
        }

        public override object Evaluate(TemplateScopeContext scope)
        {
            var result = Argument.Evaluate(scope);
            var afterUnary = Operator.Evaluate(result);
            return afterUnary;
        }
    }
}