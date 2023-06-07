using System;
using System.Collections.Generic;

namespace ServiceStack.Script;

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

    private string EvalProperty(JsToken token, ScriptScopeContext scope)
    {
        switch (token)
        {
            case JsIdentifier id:
                return id.Name;
            default:
                return JsonValue(token.Evaluate(scope));
        }
    }
        
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
            var target = memberExpr.Object.Evaluate(scope);
            if (target == null)
                throw new ArgumentNullException(memberExpr.Object.ToRawString());

            // Evaluate target then reduce to simple expression then compile assign expression using expression trees
            var assignTargetExpr = memberExpr.Computed
                ? "obj[" + EvalProperty(memberExpr.Property, scope) + "]"
                : "obj." + EvalProperty(memberExpr.Property, scope);

            if (assignFn == null)
            {
                assignFn = scope.Context.GetAssignExpression(target.GetType(), assignTargetExpr.AsMemory());
                if (assignFn == null)
                    throw new NotSupportedException($"Could not create assignment expression for '{memberExpr.ToRawString()}'");
            }

            if (assignFn != null)
            {
                assignFn(scope, target, rhs);
                return rhs;
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