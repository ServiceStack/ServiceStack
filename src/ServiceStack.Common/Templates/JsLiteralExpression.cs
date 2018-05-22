namespace ServiceStack.Templates
{
    public class JsLiteralExpression : JsExpression
    {
        public JsToken Value { get; set; }
        public override string ToRawString() => JsonValue(Value);
        public JsLiteralExpression() { }

        public JsLiteralExpression(JsToken target)
        {
            Value = target;
        }

        public override object Evaluate(TemplateScopeContext scope)
        {
            var result = scope.EvaluateToken(Value);
            return result;
        }
    }
}