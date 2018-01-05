using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;
#if NETSTANDARD2_0
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public abstract class ConditionExpression : JsToken
    {
        public virtual bool Evaluate(TemplateScopeContext scope) => false;
    }

    public abstract class BooleanExpression : ConditionExpression
    {
        public const string OrKeyword = "or";
        public const string OrSyntax = "||";
        public const string AndKeyword = "and";
        public const string AndSyntax = "&&";
        
        public string Keyword { get; }
        public string Syntax { get; }

        protected BooleanExpression(string keyword, string syntax, IEnumerable<ConditionExpression> expressions=null)
        {
            Keyword = keyword;
            Syntax = syntax;
            Expressions = expressions?.ToList() ?? new List<ConditionExpression>();
        }

        public List<ConditionExpression> Expressions { get; set; }
        
        public override string ToRawString()
        {
            var sb = StringBuilderCache.Allocate()
                .Append('(');
            
            for (var i = 0; i < Expressions.Count; i++)
            {
                var expr = Expressions[i];
                if (i > 0)
                    sb.Append(' ').Append(Keyword).Append(' ');
                
                sb.Append(JsonValue(expr));
            }
            sb.Append(')');
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public bool MatchesBinding(JsBinding binding) => binding.Binding.HasValue && (binding.Binding.Equals(Keyword) || binding.Binding.Equals(Syntax));

        protected bool Equals(BooleanExpression other) => string.Equals(Keyword, other.Keyword) && string.Equals(Syntax, other.Syntax) 
            && Expressions.EquivalentTo(other.Expressions);
        
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BooleanExpression) obj);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Keyword != null ? Keyword.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Syntax != null ? Syntax.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Expressions != null ? Expressions.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
    public class OrExpression : BooleanExpression
    {
        public OrExpression(params ConditionExpression[] expressions) : base(OrKeyword, OrSyntax, expressions){}

        public override bool Evaluate(TemplateScopeContext scope)
        {
            foreach (var expr in Expressions)
            {
                var result = expr.Evaluate(scope);
                if (result)
                    return true;
            }
            return false;
        }
    }
    public class AndExpression : BooleanExpression
    {
        public AndExpression(params ConditionExpression[] expressions) : base(AndKeyword, AndSyntax, expressions){}

        public override bool Evaluate(TemplateScopeContext scope)
        {
            foreach (var expr in Expressions)
            {
                var result = expr.Evaluate(scope);
                if (!result)
                    return false;
            }
            return true;
        }
    }

    public class UnaryExpression : ConditionExpression
    {
        public JsUnaryOperator Op { get; set; }
        public JsToken Target { get; set; }
        public override string ToRawString() => Op.Token + JsonValue(Target);
        public UnaryExpression() {}
        public UnaryExpression(JsUnaryOperator op, JsToken target)
        {
            Op = op;
            Target = target;
        }
        protected bool Equals(UnaryExpression other) => Equals(Op, other.Op) && Equals(Target, other.Target);
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((UnaryExpression) obj);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Op != null ? Op.GetHashCode() : 0) * 397) ^ (Target != null ? Target.GetHashCode() : 0);
            }
        }

        public override bool Evaluate(TemplateScopeContext scope)
        {
            var result = scope.EvaluateToken(Target);
            var afterUnary = Op.Evaluate(result);
            return (bool)afterUnary;
        }
    }
    public class BinaryExpression : ConditionExpression
    {
        public BinaryExpression() {}
        public BinaryExpression(JsToken left, JsBooleanOperand operand, JsToken right)
        {
            Left = left;
            Operand = operand;
            Right = right;
        }

        public JsBooleanOperand Operand { get; set; }
        public JsToken Left  { get; set; }
        public JsToken Right { get; set; }
        public override string ToRawString() => JsonValue(Left) + Operand.Token + JsonValue(Right);

        protected bool Equals(BinaryExpression other) => Equals(Operand, other.Operand) && Equals(Left, other.Left) && Equals(Right, other.Right);
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BinaryExpression) obj);
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

        public override bool Evaluate(TemplateScopeContext scope)
        {
            var lhs = scope.EvaluateToken(Left);
            var rhs = scope.EvaluateToken(Right);
            return Operand.Test(lhs, rhs);
        }
    }

    public static class QueryExpression
    {
        public static JsToken ToToken(this object value, JsBinding binding)
        {
            if (binding != null)
                return binding;
            if (value is JsToken t)
                return t;
            return new JsConstant(value);
        }
        
        public static StringSegment ParseConditionExpression(this string literal, out ConditionExpression expr) =>
            literal.ToStringSegment().ParseConditionExpression(out expr);
        
        public static StringSegment ParseConditionExpression(this StringSegment literal, out ConditionExpression expr)
        {
            BooleanExpression rootExpr = null;
            BinaryExpression lhsBinaryExpr = null, rhsBinaryExpr = null;
            
            literal = literal.AdvancePastWhitespace();
            while (!literal.IsNullOrEmpty())
            {
                literal = literal.ParseBinaryExpression(out lhsBinaryExpr);

                literal = literal.AdvancePastWhitespace();

                if (literal.IsNullOrEmpty() && rootExpr == null)
                {
                    expr = lhsBinaryExpr;
                    return literal;
                }

                literal = literal.ParseNextToken(out object value, out JsBinding andOrToken);

                if (andOrToken == null || !andOrToken.Binding.Equals(BooleanExpression.OrKeyword) && !andOrToken.Binding.Equals(BooleanExpression.AndKeyword))
                    throw new NotSupportedException($"Invalid sytnax: Expected 'and', 'or' keywords but found instead '{value ?? andOrToken}', near '{literal.SubstringWithElipsis(0,50)}'");
                
                var isOr = andOrToken.Binding.Equals(BooleanExpression.OrKeyword);

                literal = literal.ParseBinaryExpression(out rhsBinaryExpr);

                if (rootExpr == null)
                {
                    rootExpr = isOr
                        ? (BooleanExpression) new OrExpression(lhsBinaryExpr)
                        : new AndExpression(lhsBinaryExpr);
                }

                if (rootExpr.MatchesBinding(andOrToken))
                {
                    rootExpr.Expressions.Add(rhsBinaryExpr);
                }
                else
                {
                    rootExpr = isOr
                        ? (BooleanExpression) new OrExpression(rootExpr, rhsBinaryExpr)
                        : new AndExpression(rootExpr, rhsBinaryExpr);
                }
            }
            
            expr = rootExpr;
            return literal;
        }

        public static StringSegment ParseBinaryExpression(this StringSegment literal, out BinaryExpression binaryExpr)
        {
            literal = literal.AdvancePastWhitespace();
            
            literal = literal.ParseNextExpression(out JsToken lhsExpr);

            if (!literal.IsNullOrEmpty())
            {
                object value;
                literal = literal.ParseNextToken(out value, out JsBinding binding);

                if (binding is JsAssignment)
                    binding = JsEquals.Operand;

                var operand = binding as JsBooleanOperand;
                
                if (operand == null)
                    throw new ArgumentException($"Invalid syntax: Expected boolean operand but instead found '{value ?? binding}' near: {literal.SubstringWithElipsis(0,50)}");
                
                literal = literal.ParseNextExpression(out JsToken rhsExpr);

                binaryExpr = new BinaryExpression(
                    lhsExpr,
                    operand,
                    rhsExpr);
            }
            else
            {
                binaryExpr = new BinaryExpression(lhsExpr, JsEquals.Operand, JsConstant.True);
            }

            return literal;
        }

        public static StringSegment ParseNextExpression(this StringSegment literal, out JsToken token)
        {
            object value1, value2;
            JsBinding binding1, binding2;

            literal = literal.AdvancePastWhitespace();
            
            literal = literal.ParseNextToken(out value1, out binding1);

            literal = literal.AdvancePastWhitespace();

            if (binding1 is JsUnaryOperator u)
            {
                literal = literal.ParseNextToken(out value2, out binding2);
                token = new UnaryExpression(u, value2.ToToken(binding2));
            }
            else
            {
                token = value1.ToToken(binding1);
            }

            return literal;
        }
    }
    

}