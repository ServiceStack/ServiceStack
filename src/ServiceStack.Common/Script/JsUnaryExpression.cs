using System.Collections.Generic;

namespace ServiceStack.Script
{
    public class JsUnaryExpression : JsExpression
    {
        public JsUnaryOperator Operator { get; }
        public JsToken Argument { get; }
        public override string ToRawString() => Operator.Token + JsonValue(Argument);

        public JsUnaryExpression(JsUnaryOperator @operator, JsToken argument)
        {
            Operator = @operator ?? throw new SyntaxErrorException($"Operator missing in Unary Expression");
            Argument = argument ?? throw new SyntaxErrorException($"Argument missing in Unary Expression");
        }

        public override object Evaluate(ScriptScopeContext scope)
        {
            var result = Argument.Evaluate(scope);
            var afterUnary = Operator.Evaluate(result);
            return afterUnary;
        }
 
        public override Dictionary<string, object> ToJsAst()
        {
            var to = new Dictionary<string, object>
            {
                ["type"] = ToJsAstType(),
                ["operator"] = Operator.Token,
                ["argument"] = Argument.ToJsAst(),
            };
            return to;
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
    }
}