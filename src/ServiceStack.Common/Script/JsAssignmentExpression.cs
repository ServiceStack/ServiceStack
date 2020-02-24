using System;
using System.Collections.Generic;

namespace ServiceStack.Script
{
    public class JsAssignmentExpression : JsExpression
    {
        public JsAssignmentExpression(JsToken left, JsAssignment @operator, JsToken right)
        {
            Left = left ?? throw new SyntaxErrorException($"Left Expression missing in Binary Expression");
            Operator = @operator ?? throw new SyntaxErrorException($"Operator missing in Binary Expression");
            Right = right ?? throw new SyntaxErrorException($"Right Expression missing in Binary Expression");
        }

        public JsAssignment Operator { get; set; }
        public JsToken Left { get; set; }
        public JsToken Right { get; set; }
        public override string ToRawString() => "(" + JsonValue(Left) + Operator.Token + JsonValue(Right) + ")";

        protected bool Equals(JsAssignmentExpression other) =>
            Equals(Operator, other.Operator) && Equals(Left, other.Left) && Equals(Right, other.Right);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsAssignmentExpression) obj);
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

        private Action<ScriptScopeContext, object, object> assignFn;

        public override object Evaluate(ScriptScopeContext scope)
        {
            var rhs = Right.Evaluate(scope);

            if (Left is JsIdentifier id)
            {
                if (scope.ScopedParams.ContainsKey(id.Name))
                {
                    scope.ScopedParams[id.Name] = rhs;
                }
                else
                {
                    scope.PageResult.Args[id.Name] = rhs;
                }
                return rhs;
            }
            else if (Left is JsMemberExpression memberExpr)
            {
                if (memberExpr.Object is JsIdentifier targetId)
                {
                    var target = scope.GetValue(targetId.Name);
                    if (target == null)
                        throw new ArgumentNullException(targetId.Name);

                    if (assignFn == null)
                    {
                        if (memberExpr.Property is JsIdentifier propName)
                        {
                            assignFn = scope.Context.GetAssignExpression(
                                target.GetType(), (targetId.Name + "." + propName.Name).AsMemory());
                        }
                        else
                        {
                            var strExpr = memberExpr.ToRawString();
                            assignFn = scope.Context.GetAssignExpression(target.GetType(), strExpr.AsMemory());
                        }
                    }
                    if (assignFn != null)
                    {
                        assignFn(scope, target, rhs);
                        return rhs;
                    }
                }
            }

            throw new NotSupportedException("Assignment Expression not supported: " + Left.ToRawString());
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