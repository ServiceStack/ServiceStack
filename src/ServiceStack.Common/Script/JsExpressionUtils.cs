using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Script
{
    public static class JsExpressionUtils
    {
        public static object GetJsExpressionAndEvaluate(this ReadOnlyMemory<char> expr, ScriptScopeContext scope, Action ifNone = null)
        {
            if (expr.IsNullOrEmpty())
            {
                ifNone?.Invoke();
                return null;
            }

            var token = expr.GetCachedJsExpression(scope);
            if (token == null)
            {
                ifNone?.Invoke();
                return null;
            }

            var result = token.Evaluate(scope);
            return result;
        }

        public static async Task<object> GetJsExpressionAndEvaluateAsync(this ReadOnlyMemory<char> expr, ScriptScopeContext scope, Action ifNone = null)
        {
            if (expr.IsNullOrEmpty())
            {
                ifNone?.Invoke();
                return TypeConstants.EmptyTask;
            }

            var token = expr.GetCachedJsExpression(scope);
            if (token == null)
            {
                ifNone?.Invoke();
                return TypeConstants.EmptyTask;
            }

            var ret = await token.EvaluateAsync(scope);
            return ret == JsNull.Value 
                ? null 
                : ret;
        }

        public static bool GetJsExpressionAndEvaluateToBool(this ReadOnlyMemory<char> expr, ScriptScopeContext scope, Action ifNone = null)
        {
            if (expr.IsNullOrEmpty())
            {
                ifNone?.Invoke();
                return false;
            }

            var token = expr.GetCachedJsExpression(scope);
            if (token == null)
            {
                ifNone?.Invoke();
                return false;
            }

            var result = token.EvaluateToBool(scope);
            return result;
        }

        public static Task<bool> GetJsExpressionAndEvaluateToBoolAsync(this ReadOnlyMemory<char> expr, ScriptScopeContext scope, Action ifNone = null)
        {
            if (expr.IsNullOrEmpty())
            {
                ifNone?.Invoke();
                return TypeConstants.FalseTask;
            }

            var token = expr.GetCachedJsExpression(scope);
            if (token == null)
            {
                ifNone?.Invoke();
                return TypeConstants.FalseTask;
            }

            return token.EvaluateToBoolAsync(scope);
        }
        
        public static JsToken GetCachedJsExpression(this ReadOnlyMemory<char> expr, ScriptScopeContext scope)
        {
            if (expr.IsEmpty)
                return null;
            
            if (scope.Context.JsTokenCache.TryGetValue(expr, out var token))
                return token;

            expr.Span.ParseJsExpression(out token);
            if (token != null)
                scope.Context.JsTokenCache[expr] = token;

            return token;
        }

        public static JsToken GetCachedJsExpression(this string expr, ScriptScopeContext scope) =>
            GetCachedJsExpression(expr.AsMemory(), scope);
        
        public static ReadOnlySpan<char> ParseJsExpression(this string literal, out JsToken token) =>
            literal.AsSpan().ParseJsExpression(out token);

        public static ReadOnlySpan<char> ParseJsExpression(this ReadOnlySpan<char> literal, out JsToken token) =>
            literal.ParseJsExpression(out token, filterExpression:false);

        public static ReadOnlySpan<char> ParseJsExpression(this ReadOnlyMemory<char> literal, out JsToken token) =>
            literal.Span.ParseJsExpression(out token, filterExpression:false);

        private const char ConditionalExpressionTestChar = '?';

        public static ReadOnlySpan<char> ParseJsExpression(this ReadOnlySpan<char> literal, out JsToken token, bool filterExpression)
        {
            var peekLiteral = literal.ParseJsToken(out var node, filterExpression:filterExpression);

            peekLiteral = peekLiteral.AdvancePastWhitespace();
            
            var peekChar = peekLiteral.SafeGetChar(0);
            if (literal.IsNullOrEmpty() || peekChar.IsExpressionTerminatorChar())
            {
                token = node;
                return peekLiteral;
            }

            if (peekChar == ConditionalExpressionTestChar && 
                peekLiteral.SafeGetChar(1) != ConditionalExpressionTestChar) // not ??
            {
                literal = peekLiteral.ParseJsConditionalExpression(node, out var expression);
                token = expression;
                return literal;
            }

            if (node is JsIdentifier identifier && peekLiteral.StartsWith("=>"))
            {
                literal = peekLiteral.ParseArrowExpressionBody(new[]{ identifier }, out var arrowExpr);
                token = arrowExpr;
                return literal;
            }

            peekLiteral = peekLiteral.AdvancePastWhitespace();

            if (!peekLiteral.IsNullOrEmpty())
            {
                if (filterExpression && peekLiteral.Length > 2)
                {
                    var char1 = peekLiteral[0];
                    var char2 = peekLiteral[1];
                    if ((char1 == '|' && char2 != '|') || (char1 == '}' && char2 == '}'))
                    {
                        token = node;
                        return peekLiteral;
                    }
                }
            }
            
            peekLiteral = peekLiteral.ParseJsBinaryOperator(out var op);
            if (op != null)
            {
                literal = literal.ParseBinaryExpression(out var expr, filterExpression);
                token = expr;

                literal = literal.AdvancePastWhitespace();
                if (literal.FirstCharEquals(ConditionalExpressionTestChar))
                {
                    literal = literal.ParseJsConditionalExpression(expr, out var conditionalExpr);
                    token = conditionalExpr;
                    return literal;
                }
                
                return literal;
            }

            literal = peekLiteral.ParseJsMemberExpression(ref node, filterExpression);

            token = node;
            return literal;
        }

        private static ReadOnlySpan<char> ParseJsConditionalExpression(this ReadOnlySpan<char> literal, JsToken test, out JsConditionalExpression expression)
        {
            literal = literal.Advance(1);

            literal = literal.ParseJsExpression(out var consequent);
            literal = literal.AdvancePastWhitespace();

            if (!literal.FirstCharEquals(':'))
                throw new SyntaxErrorException($"Expected Conditional ':' but was {literal.DebugFirstChar()}");

            literal = literal.Advance(1);

            literal = literal.ParseJsExpression(out var alternate);

            expression = new JsConditionalExpression(test, consequent, alternate);
            return literal;
        }

        public static ReadOnlySpan<char> ParseBinaryExpression(this ReadOnlySpan<char> literal, out JsExpression expr, bool filterExpression)
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
                    throw new SyntaxErrorException($"Expected binary operator near: {literal.DebugLiteral()}");

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
                        if (filterExpression && literal.Length > 2 && (literal[0] == '|' && literal[1] != '|'))
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
                            throw new SyntaxErrorException($"Expected expression near: '{literal.DebugLiteral()}'");

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

        static int GetNextBinaryPrecedence(this ReadOnlySpan<char> literal)
        {
            if (!literal.IsNullOrEmpty() && !literal[0].IsExpressionTerminatorChar())
            {
                literal.ParseJsBinaryOperator(out var binaryOp);
                if (binaryOp != null)
                    return JsTokenUtils.GetBinaryPrecedence(binaryOp.Token);
            }

            return 0;
        }
    }
    

}