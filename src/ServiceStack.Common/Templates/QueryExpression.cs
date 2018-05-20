using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.DataAnnotations;
using ServiceStack.Text;
using ServiceStack.Text.Json;

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
    public class BinaryExpression : JsToken
    {
        public BinaryExpression() {}
        public BinaryExpression(JsToken left, JsBinaryOperator operand, JsToken right)
        {
            Left = left;
            Operand = operand;
            Right = right;
        }

        public JsBinaryOperator Operand { get; set; }
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

        public object Evaluate(TemplateScopeContext scope)
        {
            var lhs = scope.EvaluateToken(Left);
            var rhs = scope.EvaluateToken(Right);            
            return Operand.Evaluate(lhs, rhs);
        }
    }

    public class BinaryConditionExpression : ConditionExpression
    {
        public JsBooleanOperand Operand { get; set; }
        public JsToken Left  { get; set; }
        public JsToken Right { get; set; }
        public override string ToRawString() => JsonValue(Left) + Operand.Token + JsonValue(Right);

        public BinaryConditionExpression() {}
        public BinaryConditionExpression(JsToken left, JsBooleanOperand operand, JsToken right)
        {
            Left = left;
            Operand = operand;
            Right = right;
        }

        public override bool Evaluate(TemplateScopeContext scope)
        {
            var lhs = scope.EvaluateToken(Left);
            var rhs = scope.EvaluateToken(Right);            
            return Operand.Test(lhs, rhs);
        }

        protected bool Equals(BinaryConditionExpression other)
        {
            return Equals(Operand, other.Operand) && Equals(Left, other.Left) && Equals(Right, other.Right);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((BinaryConditionExpression) obj);
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

            literal = literal.AdvancePastWhitespace();
            while (!literal.IsNullOrEmpty())
            {
                literal = literal.ParseBinaryConditionExpression(out BinaryConditionExpression lhsConditionExpr);

                literal = literal.AdvancePastWhitespace();

                if (literal.IsNullOrEmpty() && rootExpr == null)
                {
                    expr = lhsConditionExpr;
                    return literal;
                }

                literal = literal.ParseNextToken(out object value, out JsBinding andOrToken);

                if (andOrToken == null || !andOrToken.Binding.Equals(BooleanExpression.OrKeyword) && !andOrToken.Binding.Equals(BooleanExpression.AndKeyword))
                    throw new NotSupportedException($"Invalid sytnax: Expected 'and', 'or' keywords but found instead '{value ?? andOrToken}', near '{literal.SubstringWithElipsis(0,50)}'");
                
                var isOr = andOrToken.Binding.Equals(BooleanExpression.OrKeyword);

                literal = literal.ParseBinaryConditionExpression(out BinaryConditionExpression rhsConditionExpr);

                if (rootExpr == null)
                {
                    rootExpr = isOr
                        ? (BooleanExpression) new OrExpression(lhsConditionExpr)
                        : new AndExpression(lhsConditionExpr);
                }

                if (rootExpr.MatchesBinding(andOrToken))
                {
                    rootExpr.Expressions.Add(rhsConditionExpr);
                }
                else
                {
                    rootExpr = isOr
                        ? (BooleanExpression) new OrExpression(rootExpr, rhsConditionExpr)
                        : new AndExpression(rootExpr, rhsConditionExpr);
                }
            }
            
            expr = rootExpr;
            return literal;
        }

        public static StringSegment ParseBinaryConditionExpression(this StringSegment literal, out BinaryConditionExpression binaryExpr)
        {
            literal = literal.AdvancePastWhitespace();
            
            literal = literal.ParseNextExpression(out JsToken lhsExpr);

            if (!literal.IsNullOrEmpty())
            {
                literal = literal.ParseNextToken(out var value, out JsBinding binding);

                if (binding is JsAssignment)
                    binding = JsEquals.Operand;

                if (!(binding is JsBooleanOperand operand))
                    throw new ArgumentException($"Invalid syntax: Expected boolean operand but instead found '{value ?? binding}' near: {literal.SubstringWithElipsis(0,50)}");
                
                literal = literal.ParseNextExpression(out JsToken rhsExpr);

                binaryExpr = new BinaryConditionExpression(
                    lhsExpr,
                    operand,
                    rhsExpr);
            }
            else
            {
                binaryExpr = new BinaryConditionExpression(lhsExpr, JsEquals.Operand, JsConstant.True);
            }

            return literal;
        }

        public static StringSegment ParseBinaryExpression(this StringSegment literal, out BinaryExpression binaryExpr)
        {
            literal = literal.AdvancePastWhitespace();
            
            literal = literal.ParseNextExpression(out JsToken lhs);

            if (literal.IsNullOrEmpty())
            {
                if (lhs is BinaryExpression lhsBinaryExpr)
                    binaryExpr = lhsBinaryExpr;
                else                
                    binaryExpr = new BinaryExpression(lhs, JsEquals.Operand, JsConstant.True);
            }
            else
            {
                literal = literal.ParseNextJsToken(out JsToken token);

                if (token is JsAssignment)
                    token = JsEquals.Operand;
                
                if (!(token is JsBinaryOperator op))
                    throw new ArgumentException(
                        $"Invalid syntax: Expected binary operand but instead found '{token}' near: {literal.SubstringWithElipsis(0, 50)}");

                var prec = JsTokenUtils.GetBinaryPrecedence(op.Token);

                if (prec > 0)
                {
                    literal = literal.ParseNextExpression(out JsToken rhs);

                    var stack = new Stack<JsToken>();
                    stack.Push(lhs);
                    stack.Push(op);
                    stack.Push(rhs);

                    var precedences = new List<int> { prec };

                    while (true)
                    {
                        prec = literal.GetNextBinaryPrecedence();
                        if (prec == 0)
                            break;

                        while ((stack.Count > 2) && prec <= precedences[precedences.Count - 1])
                        {
                            rhs = stack.Pop();
                            var operand = (JsBinaryOperator)stack.Pop();
                            precedences.RemoveAt(precedences.Count - 1);
                            lhs = stack.Pop();
                            stack.Push(new BinaryExpression(lhs, operand, rhs));
                        }

                        literal = literal.ParseNextJsToken(out var opToken);
                        
                        if (literal.IsNullOrEmpty())
                            throw new ArgumentException($"Invalid syntax: Expected expression after '{token}'");

                        literal = literal.ParseNextJsToken(out token);
                        
                        stack.Push(opToken);
                        stack.Push(token);
                        precedences.Add(prec);
                    }

                    var i = stack.Count - 1;
                    var ret = stack.Pop();

                    while (stack.Count > 0)
                    {
                        op = (JsBinaryOperator) stack.Pop();
                        lhs = stack.Pop();
                        ret = new BinaryExpression(lhs, op, ret);
                    }

                    binaryExpr = (BinaryExpression) ret;
                }
                else
                {
                    if (lhs is BinaryExpression lhsBinaryExpr)
                        binaryExpr = lhsBinaryExpr;
                    else                
                        binaryExpr = new BinaryExpression(lhs, JsEquals.Operand, JsConstant.True);
                }
            }

            return literal;
        }

        static int GetNextBinaryPrecedence(this StringSegment literal)
        {
            literal.ParseNextJsToken(out var token);

            if (token is JsBinaryOperator binaryOp)
            {
                return JsTokenUtils.GetBinaryPrecedence(binaryOp.Token);
            }

            return 0;
        }

        public static StringSegment ParseNextExpression(this StringSegment literal, out JsToken token)
        {
            literal = literal.AdvancePastWhitespace();

            var c = literal.GetChar(0);
            if (c == '(')
            {
                literal = literal.Advance(1);
                literal = literal.ParseBinaryExpression(out var binaryExpr);
                literal = literal.AdvancePastWhitespace();

                c = literal.GetChar(0);
                if (c == ')')
                {
                    literal = literal.Advance(1);
                    token = binaryExpr;
                    return literal;
                }
                
                throw new ArgumentException($"Invalid syntax: Expected ')' but instead found '{c}': {literal.SubstringWithElipsis(0, 50)}");
            }
            
            literal = literal.ParseNextToken(out var value1, out var binding1);

            literal = literal.AdvancePastWhitespace();

            if (binding1 is JsUnaryOperator u)
            {
                literal = literal.ParseNextToken(out var value2, out var binding2);
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