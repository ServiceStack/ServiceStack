namespace ServiceStack.Templates
{
    public class JsUnaryExpression : JsExpression
    {
        public JsUnaryOperator Op { get; }
        public JsToken Target { get; }
        public override string ToRawString() => Op.Token + JsonValue(Target);

        public JsUnaryExpression(JsUnaryOperator op, JsToken target)
        {
            Op = op;
            Target = target;
        }

        protected bool Equals(JsUnaryExpression other) => Equals(Op, other.Op) && Equals(Target, other.Target);

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
                return ((Op != null ? Op.GetHashCode() : 0) * 397) ^ (Target != null ? Target.GetHashCode() : 0);
            }
        }

        public override object Evaluate(TemplateScopeContext scope)
        {
            var result = Target.Evaluate(scope);
            var afterUnary = Op.Evaluate(result);
            return afterUnary;
        }
    }
}