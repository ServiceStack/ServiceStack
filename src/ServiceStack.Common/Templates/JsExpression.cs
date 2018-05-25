using System;
using System.Collections.Generic;
using ServiceStack.Text;

#if NETSTANDARD2_0
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public abstract class JsExpression : JsToken {}

    public static class JsExpressionUtils
    {
        public static StringSegment ParseJsExpression(this string literal, out JsToken token) =>
            literal.ToStringSegment().ParseJsExpression(out token);

        public static StringSegment ParseJsExpression(this StringSegment literal, out JsToken token) =>
            literal.ParseJsExpression(out token, filterExpression:false);

        public static StringSegment ParseJsExpression(this StringSegment literal, out JsToken token, bool filterExpression)
        {
            var peekLiteral = literal.ParseJsToken(out var token1, filterExpression:filterExpression);

            peekLiteral = peekLiteral.AdvancePastWhitespace();
            
            if (peekLiteral.IsNullOrEmpty())
            {
                token = token1;
                return peekLiteral;
            }

            var peekChar = peekLiteral.GetChar(0);
            if (peekChar.IsExpressionTerminatorChar())
            {
                token = token1;
                return peekLiteral;
            }

            var isConditionalExpression = peekChar == '?';
            if (isConditionalExpression)
            {
                literal = peekLiteral.Advance(1);

                literal = literal.ParseJsExpression(out var consequent);
                literal = literal.AdvancePastWhitespace();
                
                if (!literal.FirstCharEquals(':'))
                    throw new SyntaxErrorException($"Expected ':' but was {literal.DebugFirstChar()}");

                literal = literal.Advance(1);

                literal = literal.ParseJsExpression(out var alternate);
                
                token = new JsConditionalExpression(token1, consequent, alternate);
                return literal;
            }

            peekLiteral = peekLiteral.AdvancePastWhitespace();

            if (!peekLiteral.IsNullOrEmpty())
            {
                if (filterExpression && peekLiteral.Length > 2)
                {
                    var char1 = peekLiteral.GetChar(0);
                    var char2 = peekLiteral.GetChar(1);
                    if ((char1 == '|' && char2 != '|') || (char1 == '}' && char2 == '}'))
                    {
                        token = token1;
                        return peekLiteral;
                    }
                }
            }
            
            peekLiteral = peekLiteral.ParseJsBinaryOperator(out var op);
            if (op != null)
            {
                literal = literal.ParseBinaryExpression(out var expr, filterExpression);
                token = expr;
                return literal;
            }

            token = token1;
            return peekLiteral;
        }
        
        public static StringSegment ParseBinaryExpression(this StringSegment literal, out JsExpression expr, bool filterExpression)
        {
            literal = literal.AdvancePastWhitespace();
            
            literal = literal.ParseJsToken(out var lhs, filterExpression:filterExpression);

            if (literal.IsNullOrEmpty())
            {
                expr = lhs is JsExpression jsExpr
                    ? jsExpr
                    : throw new SyntaxErrorException($"Expected Expression but was {lhs.DebugToken()}");
            }
            else
            {
                literal = literal.ParseJsBinaryOperator(out var op);

                if (op == null)
                    throw new SyntaxErrorException($"Expected binary operator near: {literal.SubstringWithElipsis(0, 50)}");

                var prec = JsTokenUtils.GetBinaryPrecedence(op.Token);
                if (prec > 0)
                {
                    literal = literal.ParseJsToken(out JsToken rhs, filterExpression:filterExpression);

                    var stack = new Stack<JsToken>();
                    stack.Push(lhs);
                    stack.Push(op);
                    stack.Push(rhs);

                    var precedences = new List<int> { prec };

                    while (true)
                    {
                        literal = literal.AdvancePastWhitespace();
                        if (filterExpression && literal.Length > 2 && (literal.GetChar(0) == '|' && literal.GetChar(1) != '|'))
                        {
                            break;
                        }

                        prec = literal.GetNextBinaryPrecedence();
                        if (prec == 0)
                            break;

                        while ((stack.Count > 2) && prec <= precedences[precedences.Count - 1])
                        {
                            rhs = stack.Pop();
                            var operand = (JsBinaryOperator)stack.Pop();
                            precedences.RemoveAt(precedences.Count - 1);
                            lhs = stack.Pop();
                            stack.Push(CreateJsExpression(lhs, operand, rhs));
                        }

                        literal = literal.ParseJsBinaryOperator(out op);
                        
                        if (literal.IsNullOrEmpty())
                            throw new SyntaxErrorException($"Expected expression near: '{literal.SubstringWithElipsis(0,40)}'");

                        literal = literal.ParseJsToken(out var token, filterExpression:filterExpression);
                        
                        stack.Push(op);
                        stack.Push(token);
                        precedences.Add(prec);
                    }

                    var i = stack.Count - 1;
                    var ret = stack.Pop();

                    while (stack.Count > 0)
                    {
                        op = (JsBinaryOperator) stack.Pop();
                        lhs = stack.Pop();
                        ret = CreateJsExpression(lhs, op, ret);
                    }

                    expr = (JsExpression) ret;
                }
                else
                {
                    expr = lhs is JsExpression jsExpr
                        ? jsExpr
                        : throw new SyntaxErrorException($"Expected Expression but was {lhs.DebugToken()}");
                }
            }

            return literal;
        }

        public static JsExpression CreateJsExpression(JsToken lhs, JsBinaryOperator op, JsToken rhs)
        {
            if (op is JsAnd opAnd)
                return new JsLogicalExpression(lhs, opAnd, rhs);
            if (op is JsOr opOr)
                return new JsLogicalExpression(lhs, opOr, rhs);
            
            return new JsBinaryExpression(lhs, op, rhs);
        }

        static int GetNextBinaryPrecedence(this StringSegment literal)
        {
            if (!literal.IsNullOrEmpty() && !literal.GetChar(0).IsExpressionTerminatorChar())
            {
                literal.ParseJsBinaryOperator(out var binaryOp);
                if (binaryOp != null)
                    return JsTokenUtils.GetBinaryPrecedence(binaryOp.Token);
            }

            return 0;
        }
    }
    

}