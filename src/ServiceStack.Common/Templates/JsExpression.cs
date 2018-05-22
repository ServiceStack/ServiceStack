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
    public abstract class JsExpression : JsToken
    {
    }

    public static class JsExpressionUtils
    {
        public static JsToken ToToken(this object value, JsBinding binding)
        {
            if (binding != null)
                return binding;
            if (value is JsToken t)
                return t;
            return new JsConstant(value);
        }
        
        public static StringSegment ParseExpression(this string literal, out JsToken token) =>
            literal.ToStringSegment().ParseExpression(out token);

        public static StringSegment ParseExpression(this StringSegment literal, out JsToken token) =>
            literal.ParseExpression(out token, filterExpression:false);

        public static StringSegment ParseExpression(this StringSegment literal, out JsToken token, bool filterExpression)
        {
            var peekLiteral = literal.ParseJsToken(out var token1, filterExpression:filterExpression);

            peekLiteral = peekLiteral.AdvancePastWhitespace();
            
            if (peekLiteral.IsNullOrEmpty())
            {
                token = token1;
                return peekLiteral;
            }

            var peekChar = peekLiteral.GetChar(0);
            if (peekChar == ')' || peekChar == ']' || peekChar == '}' || peekChar == ',')
            {
                token = token1;
                return peekLiteral;
            }
            
            if (token1 is JsSubtraction)
                token1 = JsMinus.Operator;
            if (token1 is JsAddition)
                token1 = JsPlus.Operator;

            if (token1 is JsUnaryOperator u)
            {
                literal = peekLiteral.ParseJsToken(out var token2, filterExpression:filterExpression);
                token = new JsUnaryExpression(u, token2);
                return literal;
            }

            peekLiteral = peekLiteral.AdvancePastWhitespace();

            if (!peekLiteral.IsNullOrEmpty())
            {
                if (filterExpression && peekLiteral.Length > 2)
                {
                    if ((peekLiteral.GetChar(0) == '|' && peekLiteral.GetChar(1) != '|') || (peekLiteral.GetChar(0) == '}' && peekLiteral.GetChar(1) == '}'))
                    {
                        token = token1;
                        return peekLiteral;
                    }
                }
            }
            
            peekLiteral = peekLiteral.ParseJsToken(out JsToken op, filterExpression:filterExpression);

            if (op is JsAssignment)
                op = JsEquals.Operator;
    
            if (op is JsBinaryOperator)
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
            
            literal = literal.ParseJsToken(out JsToken lhs, filterExpression:filterExpression);

            JsExpression CreateSingleExpression(JsToken left)
            {
                if (left is JsExpression jsExpr)
                    return jsExpr;
                return new JsLiteralExpression(left);
            }

            if (literal.IsNullOrEmpty())
            {
                expr = CreateSingleExpression(lhs);
            }
            else
            {
                literal = literal.ParseJsToken(out JsToken token, filterExpression:filterExpression);

                if (token is JsAssignment)
                    token = JsEquals.Operator;
                
                if (!(token is JsBinaryOperator op))
                    throw new ArgumentException(
                        $"Invalid syntax: Expected binary operand but instead found '{token}' near: {literal.SubstringWithElipsis(0, 50)}");

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

                        literal = literal.ParseJsToken(out var opToken, filterExpression:filterExpression);
                        
                        if (literal.IsNullOrEmpty())
                            throw new ArgumentException($"Invalid syntax: Expected expression after '{token}'");

                        literal = literal.ParseJsToken(out token, filterExpression:filterExpression);
                        
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
                        ret = CreateJsExpression(lhs, op, ret);
                    }

                    expr = (JsExpression) ret;
                }
                else
                {
                    expr = CreateSingleExpression(lhs);
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
            literal.ParseJsToken(out var token);

            if (token is JsBinaryOperator binaryOp)
            {
                return JsTokenUtils.GetBinaryPrecedence(binaryOp.Token);
            }

            return 0;
        }
    }
    

}