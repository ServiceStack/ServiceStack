using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Text.Json;

namespace ServiceStack.Script
{
    public abstract class JsToken : IRawString
    {
        public abstract string ToRawString();

        public string JsonValue(object value)
        {
            if (value == null || value == JsNull.Value)
                return "null";
            if (value is JsToken jt)
                return jt.ToRawString();
            if (value is string s)
                return s.EncodeJson();
            return value.ToString();
        }

        public override string ToString() => ToRawString();

        public abstract object Evaluate(ScriptScopeContext scope);
            
        public static object UnwrapValue(JsToken token)
        {
            if (token is JsLiteral literal)
                return literal.Value;
            return null;
        }
    }

    public static class JsNull
    {
        public const string String = "null";
        public static JsLiteral Value = new JsLiteral(null);
    }

    public static class JsTokenUtils
    {
        private static readonly byte[] ValidNumericChars;
        private static readonly byte[] ValidVarNameChars;
        private static readonly byte[] OperatorChars;
        private static readonly byte[] ExpressionTerminatorChars;
        private const byte True = 1;
        
        static JsTokenUtils()
        {
            var n = new byte['e' + 1];
            n['0'] = n['1'] = n['2'] = n['3'] = n['4'] = n['5'] = n['6'] = n['7'] = n['8'] = n['9'] = n['.'] = True;
            ValidNumericChars = n;

            var o = new byte['~' + 1];
            o['<'] = o['>'] = o['='] = o['!'] = o['+'] = o['-'] = o['*'] = o['/'] = o['%'] = o['|'] = o['&'] = o['^'] = o['~'] = o['?'] = True;
            OperatorChars = o;

            var e = new byte['}' + 1];
            e[')'] = e['}'] = e[';'] = e[','] = e[']'] = e[':'] = True;
            ExpressionTerminatorChars = e;

            var a = new byte['z' + 1];
            for (var i = (int) '0'; i < a.Length; i++)
            {
                if (i >= 'A' && i <= 'Z' || i >= 'a' && i <= 'z' || i >= '0' && i <= '9' || i == '_')
                    a[i] = True;
            }
            ValidVarNameChars = a;
        }

        public static readonly Dictionary<string, int> OperatorPrecedence = new Dictionary<string, int> {
            {")", 0},
            {";", 0},
            {",", 0},
            {"=", 0},
            {"]", 0},
            {"??", 1},
            {"||", 1},
            {"&&", 2},
            {"|", 3},
            {"^", 4},
            {"&", 5},
            {"==", 6},
            {"!=", 6},
            {"===", 6},
            {"!==", 6},
            {"<", 7},
            {">", 7},
            {"<=", 7},
            {">=", 7},
            {"<<", 8},
            {">>", 8},
            {">>>", 8},
            {"+", 9},
            {"-", 9},
            {"*", 11},
            {"/", 11},
            {"%", 11},
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBinaryPrecedence(string token) => OperatorPrecedence.TryGetValue(token, out var precedence) ? precedence : 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumericChar(this char c) => c < ValidNumericChars.Length && ValidNumericChars[c] == True;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidVarNameChar(this char c) => c < ValidVarNameChars.Length && ValidVarNameChars[c] == True;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOperatorChar(this char c) => c < OperatorChars.Length && OperatorChars[c] == True;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsExpressionTerminatorChar(this char c) => c < ExpressionTerminatorChars.Length && ExpressionTerminatorChars[c] == True;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static JsUnaryOperator GetUnaryOperator(this char c)
        {
            switch (c)
            {
                case '-':
                    return JsMinus.Operator;
                case '+':
                    return JsPlus.Operator;
                case '!':
                    return JsNot.Operator;
                case '~':
                    return JsBitwiseNot.Operator;
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FirstCharEquals(this ReadOnlySpan<char> literal, char c) => 
            !literal.IsNullOrEmpty() && literal[0] == c;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FirstCharEquals(this string literal, char c) => 
            !string.IsNullOrEmpty(literal) && literal[0] == c;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char SafeGetChar(this ReadOnlySpan<char> literal, int index) =>
            index >= 0 && index < literal.Length ? literal[index] : default(char);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SafeCharEquals(this ReadOnlySpan<char> literal, int index, char c) =>
            index >= 0 && index < literal.Length && literal[index] == c;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char SafeGetChar(this ReadOnlyMemory<char> literal, int index) => literal.Span.SafeGetChar(index);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnd(this char c) => c == default(char);

        // Remove `Js` prefix
        public static string ToJsAstType(this Type type) => type.Name.Substring(2);

        public static Dictionary<string, object> ToJsAst(this JsToken token) => token is JsExpression expression
            ? expression.ToJsAst()
            : throw new NotSupportedException(token.GetType().Name + " is not a JsExpression");

        public static string ToJsAstString(this JsToken token)
        {
            using (JsConfig.With(new Config { IncludeNullValuesInDictionaries = true }))
            {
                return token.ToJsAst().ToJson().IndentJson();
            }
        }

        internal static string DebugFirstChar(this ReadOnlySpan<char> literal) => literal.IsNullOrEmpty()
            ? "<end>"
            : $"'{literal[0]}'";

        internal static string DebugFirstChar(this ReadOnlyMemory<char> literal) => literal.Span.DebugFirstChar();

        internal static string DebugChar(this char c) => c == 0 ? "'<end>'" : $"'{c}'";

        internal static string DebugToken(this JsToken token) => $"'{token}'";
        
        internal static string DebugLiteral(this ReadOnlySpan<char> literal) => $"'{literal.SubstringWithEllipsis(0, 50)}'";
        
        internal static string DebugLiteral(this ReadOnlyMemory<char> literal) => $"'{literal.Span.SubstringWithEllipsis(0, 50)}'";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string CookRawString(this ReadOnlySpan<char> str, char quoteChar) =>
            JsonTypeSerializer.UnescapeJsString(str, quoteChar).Value() ?? "";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlyMemory<char> TrimFirstNewLine(this ReadOnlyMemory<char> literal) => literal.StartsWith("\r\n")
            ? literal.Advance(2)
            : (literal.StartsWith("\n") ? literal.Advance(1) : literal);

        public static object Evaluate(this JsToken token) => token.Evaluate(JS.CreateScope());

        public static bool EvaluateToBool(this JsToken token, ScriptScopeContext scope)
        {
            var ret = token.Evaluate(scope);
            if (ret is bool b)
                return b;

            return !DefaultScripts.isFalsy(ret);
        }

        /// <summary>
        /// Evaulate if result can be async, if so converts async result to Task&lt;object&gt; otherwise wraps result in a Task
        /// </summary>
        public static async Task<bool> EvaluateToBoolAsync(this JsToken token, ScriptScopeContext scope)
        {
            var ret = await token.EvaluateAsync(scope);
            if (ret is bool b)
                return b;

            return !DefaultScripts.isFalsy(ret);
        }

        /// <summary>
        /// Evaulate if result can be async, if so converts async result to Task&lt;object&gt; otherwise wraps result in a Task
        /// </summary>
        public static bool EvaluateToBool(this JsToken token, ScriptScopeContext scope, out bool? result, out Task<bool> asyncResult)
        {
            if (token.Evaluate(scope, out var oResult, out var oAsyncResult))
            {
                result = oResult is bool b ? b : !DefaultScripts.isFalsy(oResult);
                asyncResult = null;
                return true;
            }

            result = null;

            var tcs = new TaskCompletionSource<bool>();
            oAsyncResult.ContinueWith(t => tcs.SetResult(!DefaultScripts.isFalsy(t.Result)), TaskContinuationOptions.OnlyOnRanToCompletion);
            oAsyncResult.ContinueWith(t => tcs.SetException(t.Exception.InnerExceptions), TaskContinuationOptions.OnlyOnFaulted);
            oAsyncResult.ContinueWith(t => tcs.SetCanceled(), TaskContinuationOptions.OnlyOnCanceled);
            asyncResult = tcs.Task;

            return false;
        }

        /// <summary>
        /// Evaulate if result can be async, if so converts async result to Task&lt;object&gt; otherwise wraps result in a Task
        /// </summary>
        public static Task<object> EvaluateAsync(this JsToken token, ScriptScopeContext scope)
        {
            var result = token.Evaluate(scope);
            if (result is Task<object> taskObj)
                return taskObj;
            if (result is Task task)
                return task.GetResult().InTask();
            return result.InTask();
        }

        /// <summary>
        /// Evaluate then set asyncResult if Result was async, otherwise set result.
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="result"></param>
        /// <param name="asyncResult"></param>
        /// <returns>true if result was synchronous otherwise false</returns>
        public static bool Evaluate(this JsToken token, ScriptScopeContext scope, out object result, out Task<object> asyncResult)
        {
            result = token.Evaluate(scope);
            if (result is Task<object> taskObj)
            {
                asyncResult = taskObj;
                result = null;
            }
            else if (result is Task task)
            {
                asyncResult = task.GetResult().InTask();
                result = null;
            }
            else
            {
                asyncResult = null;
                return true;
            }
            return false;
        }
        
        public static ReadOnlySpan<char> ParseJsToken(this ReadOnlySpan<char> literal, out JsToken token) => ParseJsToken(literal, out token, false);
        public static ReadOnlySpan<char> ParseJsToken(this ReadOnlySpan<char> literal, out JsToken token, bool filterExpression)
        {
            literal = literal.AdvancePastWhitespace();

            if (literal.IsNullOrEmpty())
            {
                token = null;
                return literal;
            }
            
            var c = literal[0];
            if (c == '(')
            {
                literal = literal.Advance(1);
                literal = literal.ParseJsExpression(out var bracketsExpr);
                literal = literal.AdvancePastWhitespace();

                if (literal.FirstCharEquals(')'))
                {
                    literal = literal.Advance(1);
                    token = bracketsExpr;
                    return literal;
                }

                if (literal.FirstCharEquals(',') && bracketsExpr is JsIdentifier param1) // (a,b,c) => ...
                {
                    literal = literal.Advance(1);
                    var args = new List<JsIdentifier> { param1, };
                    while (true)
                    {
                        literal = literal.AdvancePastWhitespace();
                        literal = literal.ParseIdentifier(out var arg);
                        if (!(arg is JsIdentifier param))
                            throw new SyntaxErrorException($"Expected identifier but was instead '{arg.DebugToken()}', near: {literal.DebugLiteral()}");

                        args.Add(param);

                        literal = literal.AdvancePastWhitespace();
                        
                        if (literal.FirstCharEquals(')'))
                            break;
                        
                        if (!literal.FirstCharEquals(','))
                            throw new SyntaxErrorException($"Expected ',' or ')' but was instead '{literal.DebugFirstChar()}', near: {literal.DebugLiteral()}");
                            
                        literal = literal.Advance(1);
                    }

                    literal = literal.Advance(1);
                    literal = literal.ParseArrowExpressionBody(args.ToArray(), out var expr);
                    token = expr;
                    return literal;
                }
                
                throw new SyntaxErrorException($"Expected ')' but instead found {literal.DebugFirstChar()} near: {literal.DebugLiteral()}");
            }

            token = null;
            c = (char) 0;

            if (literal.IsNullOrEmpty())
                return default;

            var i = 0;
            literal = literal.AdvancePastWhitespace();

            var firstChar = literal[0];
            if (firstChar == '\'' || firstChar == '"' || firstChar == '`' || firstChar == '′')
            {
                var quoteChar = firstChar;
                i = 1;
                var hasEscapeChar = false;
                
                while (i < literal.Length)
                {
                    c = literal[i];
                    if (c == quoteChar)
                    {
                        if (!literal.SafeCharEquals(i - 1,'\\') ||
                            (literal.SafeCharEquals(i - 2,'\\') && !literal.SafeCharEquals(i - 3,'\\')))
                            break;
                    }
                    
                    i++;
                    if (!hasEscapeChar)
                        hasEscapeChar = c == '\\';
                }

                if (i >= literal.Length || literal[i] != quoteChar)
                    throw new SyntaxErrorException($"Unterminated string literal: {literal.ToString()}");

                var rawString = literal.Slice(1, i - 1);

                if (quoteChar == '`')
                {
                    token = ParseJsTemplateLiteral(rawString);
                }
                else if (hasEscapeChar)
                {
                    if (quoteChar == '′')
                    {
                        //All other quoted strings use unescaped strings  
                        var sb = StringBuilderCache.Allocate();
                        for (var j = 0; j < rawString.Length; j++)
                        {
                            // strip the back-slash used to escape quote char in strings
                            var ch = rawString[j];
                            if (ch != '\\' || (j + 1 >= rawString.Length || rawString[j + 1] != quoteChar))
                                sb.Append(ch);
                        }
                        token = new JsLiteral(StringBuilderCache.ReturnAndFree(sb));
                    }
                    else
                    {
                        var unescapedString = JsonTypeSerializer.Unescape(rawString);
                        token = new JsLiteral(unescapedString.ToString());
                    }
                }
                else
                {
                    token = new JsLiteral(rawString.ToString());
                }
                
                return literal.Advance(i + 1);
            }

            if (firstChar >= '0' && firstChar <= '9')
            {
                i = 1;
                var hasExponent = false;
                var hasDecimal = false;

                while (i < literal.Length && IsNumericChar(c = literal[i]) ||
                       (hasExponent = (c == 'e' || c == 'E')))
                {
                    if (c == '.')
                        hasDecimal = true;

                    i++;

                    if (hasExponent)
                    {
                        i += 2; // [e+1]0

                        while (i < literal.Length && IsNumericChar(literal[i]))
                            i++;

                        break;
                    }
                }

                var numLiteral = literal.Slice(0, i);

                //don't convert into ternary to avoid Type coercion
                if (hasDecimal || hasExponent)
                    token = new JsLiteral(numLiteral.TryParseDouble(out double d) ? d : default(double));
                else
                    token = new JsLiteral(numLiteral.ParseSignedInteger());

                return literal.Advance(i);
            }
            if (firstChar == '{')
            {
                var props = new List<JsProperty>();

                literal = literal.Advance(1);
                while (!literal.IsNullOrEmpty())
                {
                    literal = literal.AdvancePastWhitespace();
                    if (literal[0] == '}')
                    {
                        literal = literal.Advance(1);
                        break;
                    }

                    JsToken mapValueToken;

                    if (literal.StartsWith("..."))
                    {
                        literal = literal.Advance(3);
                        literal = literal.ParseJsExpression(out mapValueToken);
                        
                        props.Add(new JsProperty(null, new JsSpreadElement(mapValueToken)));
                    }
                    else
                    {
                        literal = literal.ParseJsToken(out var mapKeyToken);

                        if (!(mapKeyToken is JsLiteral) && !(mapKeyToken is JsTemplateLiteral) && !(mapKeyToken is JsIdentifier) && !(mapKeyToken is JsMemberExpression)) 
                            throw new SyntaxErrorException($"{mapKeyToken.DebugToken()} is not a valid Object key, expected literal, identifier or member expression.");
    
                        bool shorthand = false;
    
                        literal = literal.AdvancePastWhitespace();
                        if (literal.Length > 0 && literal[0] == ':')
                        {
                            literal = literal.Advance(1);
                            literal = literal.ParseJsExpression(out mapValueToken);
    
                        }
                        else 
                        {
                            shorthand = true;
                            if (literal.Length == 0 || (c = literal[0]) != ',' && c != '}')
                                throw new SyntaxErrorException($"Unterminated object literal near: {literal.DebugLiteral()}");
                                
                            mapValueToken = mapKeyToken;
                        }
                        
                        props.Add(new JsProperty(mapKeyToken, mapValueToken, shorthand));
                    }

                    literal = literal.AdvancePastWhitespace();
                    if (literal.IsNullOrEmpty())
                        break;

                    if (literal[0] == '}')
                    {
                        literal = literal.Advance(1);
                        break;
                    }

                    literal = literal.AdvancePastChar(',');
                    literal = literal.AdvancePastWhitespace();
                }

                token = new JsObjectExpression(props);
                return literal;
            }
            if (firstChar == '[')
            {
                literal = literal.Advance(1);
                literal = literal.ParseArguments(out var elements, termination: ']');

                token = new JsArrayExpression(elements);
                return literal;
            }

            var unaryOp = firstChar.GetUnaryOperator();
            if (unaryOp != null)
            {
                literal = literal.Advance(1);
                literal = literal.ParseJsToken(out var arg);
                token = new JsUnaryExpression(unaryOp, arg);
                return literal;
            }
            
            // identifier
            literal = literal.ParseIdentifier(out var node);

            if (!(node is JsOperator))
            {
                literal = literal.ParseJsMemberExpression(ref node, filterExpression);
            }

            token = node;
            return literal;
        }

        private static JsToken ParseJsTemplateLiteral(ReadOnlySpan<char> literal)
        {
            var quasis = new List<JsTemplateElement>();
            var expressions = new List<JsToken>();
            var lastPos = 0;

            for (var i = 0; i < literal.Length; i++)
            {
                var c = literal[i];
                if (c != '$' || literal.SafeGetChar(i-1) == '\\' || literal.SafeGetChar(i+1) != '{')
                    continue;

                var lastChunk = literal.Slice(lastPos, i - lastPos);
                quasis.Add(new JsTemplateElement(
                    new JsTemplateElementValue(lastChunk.ToString(), lastChunk.CookRawString('`')), 
                    tail:false));

                var exprStart = literal.Slice(i + 2);
                var afterExpr = exprStart.ParseJsExpression(out var expr);
                afterExpr = afterExpr.AdvancePastWhitespace();
                
                if (!afterExpr.FirstCharEquals('}'))
                    throw new SyntaxErrorException($"Expected end of template literal expression '}}' but was instead {literal.DebugFirstChar()}");
                afterExpr = afterExpr.Advance(1);
                
                expressions.Add(expr);

                lastPos = literal.Length - afterExpr.Length;
                i = lastPos - 1;
            }

            var endChunk = literal.Slice(lastPos);

            quasis.Add(new JsTemplateElement(
                new JsTemplateElementValue(endChunk.ToString(), endChunk.CookRawString('`')), 
                tail:true));
            
            return new JsTemplateLiteral(quasis.ToArray(), expressions.ToArray());
        }

        internal static ReadOnlySpan<char> ParseArrowExpressionBody(this ReadOnlySpan<char> literal, JsIdentifier[] args, out JsArrowFunctionExpression token)
        {
            literal = literal.AdvancePastWhitespace();
            
            if (!literal.StartsWith("=>"))
                throw new SyntaxErrorException($"Expected '=>' but instead found {literal.DebugFirstChar()} near: {literal.DebugLiteral()}");

            literal = literal.Advance(2);
            literal = literal.ParseJsExpression(out var body, filterExpression:true);
            token = new JsArrowFunctionExpression(args, body);
            return literal;
        }

        internal static ReadOnlySpan<char> ParseJsMemberExpression(this ReadOnlySpan<char> literal, ref JsToken node, bool filterExpression)
        {
            literal = literal.AdvancePastWhitespace();

            if (literal.IsNullOrEmpty())
                return literal;
            
            var c = literal[0];

            while (c == '.' || c == '[' || c == '(' || (filterExpression && c == ':'))
            {
                literal = literal.Advance(1);

                if (c == '.')
                {
                    literal = literal.AdvancePastWhitespace();
                    literal = literal.ParseIdentifier(out var property);
                    node = new JsMemberExpression(node, property, computed: false);
                }
                else if (c == '[')
                {
                    literal = literal.AdvancePastWhitespace();
                    literal = literal.ParseJsExpression(out var property);
                    node = new JsMemberExpression(node, property, computed: true);

                    literal = literal.AdvancePastWhitespace();
                    if (!literal.FirstCharEquals(']'))
                        throw new SyntaxErrorException($"Expected ']' but was {literal.DebugFirstChar()}");

                    literal = literal.Advance(1);
                }
                else if (c == '(')
                {
                    literal = literal.ParseArguments(out var args, termination: ')');
                    node = new JsCallExpression(node, args.ToArray());
                }
                else if (filterExpression)
                {
                    if (c == ':')
                    {
                        literal = literal.ParseWhitespaceArgument(out var argument);
                        node = new JsCallExpression(node, argument);
                        return literal;
                    }

                    var peekLiteral = literal.AdvancePastWhitespace();
                    if (peekLiteral.StartsWith("=>"))
                    {
                        literal = peekLiteral.ParseArrowExpressionBody(new[]{ new JsIdentifier("it") }, out var arrowExpr);
                        node = arrowExpr;
                        return literal;
                    }
                }

                literal = literal.AdvancePastWhitespace();
                    
                if (literal.IsNullOrEmpty())
                    break;

                c = literal[0];
            }

            return literal;
        }

        internal static ReadOnlySpan<char> ParseJsBinaryOperator(this ReadOnlySpan<char> literal, out JsBinaryOperator op)
        {
            literal = literal.AdvancePastWhitespace();
            op = null;
            
            if (literal.IsNullOrEmpty())
                return literal;
            
            var firstChar = literal[0];
            if (firstChar.IsOperatorChar())
            {
                if (literal.StartsWith("!=="))
                {
                    op = JsStrictNotEquals.Operator;
                    return literal.Advance(3);
                }
                if (literal.StartsWith("==="))
                {
                    op = JsStrictEquals.Operator;
                    return literal.Advance(3);
                }

                if (literal.StartsWith(">="))
                {
                    op = JsGreaterThanEqual.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("<="))
                {
                    op = JsLessThanEqual.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("!="))
                {
                    op = JsNotEquals.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("=="))
                {
                    op = JsEquals.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("||"))
                {
                    op = JsOr.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("??"))
                {
                    op = JsCoalescing.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("&&"))
                {
                    op = JsAnd.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("<<"))
                {
                    op = JsBitwiseLeftShift.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith(">>"))
                {
                    op = JsBitwiseRightShift.Operator;
                    return literal.Advance(2);
                }

                switch (firstChar)
                {
                    case '>':
                        op = JsGreaterThan.Operator;
                        return literal.Advance(1);
                    case '<':
                        op = JsLessThan.Operator;
                        return literal.Advance(1);
                    case '=':
                        op = JsEquals.Operator;
                        return literal.Advance(1);
                    case '+':
                        op = JsAddition.Operator;
                        return literal.Advance(1);
                    case '-':
                        op = JsSubtraction.Operator;
                        return literal.Advance(1);
                    case '*':
                        op = JsMultiplication.Operator;
                        return literal.Advance(1);
                    case '/':
                        op = JsDivision.Operator;
                        return literal.Advance(1);
                    case '&':
                        op = JsBitwiseAnd.Operator;
                        return literal.Advance(1);
                    case '|':
                        op = JsBitwiseOr.Operator;
                        return literal.Advance(1);
                    case '^':
                        op = JsBitwiseXOr.Operator;
                        return literal.Advance(1);
                    case '%':
                        op = JsMod.Operator;
                        return literal.Advance(1);
                    
                    case '?': // a single '?' is not a binary operator but is an op char used in '??'
                        return literal;
                    default:
                        throw new SyntaxErrorException($"Invalid Operator found near: {literal.DebugLiteral()}");
                }
            }

            if (literal.StartsWith("and") && literal.SafeGetChar(3).IsWhiteSpace())
            {
                op = JsAnd.Operator;
                return literal.Advance(3);
            }

            if (literal.StartsWith("or") && literal.SafeGetChar(2).IsWhiteSpace())
            {
                op = JsOr.Operator;
                return literal.Advance(2);
            }

            return literal;
        }

        public static ReadOnlySpan<char> ParseVarName(this ReadOnlySpan<char> literal, out ReadOnlySpan<char> varName)
        {
            literal = literal.AdvancePastWhitespace();

            var c = literal.SafeGetChar(0);
            if (!c.IsValidVarNameChar())
                throw new SyntaxErrorException($"Expected start of identifier but was {c.DebugChar()} near: {literal.DebugLiteral()}");

            var i = 1;
            
            while (i < literal.Length)
            {
                c = literal[i];

                if (IsValidVarNameChar(c))
                    i++;
                else
                    break;
            }

            varName = literal.Slice(0, i).TrimEnd();
            literal = literal.Advance(i);

            return literal;
        }

        public static ReadOnlyMemory<char> ParseVarName(this ReadOnlyMemory<char> literal, out ReadOnlyMemory<char> varName)
        {
            literal = literal.AdvancePastWhitespace();

            var c = literal.SafeGetChar(0);
            if (!c.IsValidVarNameChar())
                throw new SyntaxErrorException($"Expected start of identifier but was {c.DebugChar()} near: {literal.DebugLiteral()}");

            var i = 1;
            
            var span = literal.Span;
            while (i < span.Length)
            {
                c = span[i];

                if (IsValidVarNameChar(c))
                    i++;
                else
                    break;
            }

            varName = literal.Slice(0, i).TrimEnd();
            literal = literal.Advance(i);

            return literal;
        }

        internal static ReadOnlySpan<char> ParseIdentifier(this ReadOnlySpan<char> literal, out JsToken token)
        {
            literal = literal.ParseVarName(out var identifier);
            
            if (identifier.EqualsOrdinal("true"))
                token = JsLiteral.True;
            else if (identifier.EqualsOrdinal("false"))
                token = JsLiteral.False;
            else if (identifier.EqualsOrdinal("null"))
                token = JsNull.Value;
            else if (identifier.EqualsOrdinal("and"))
                token = JsAnd.Operator;
            else if (identifier.EqualsOrdinal("or"))
                token = JsOr.Operator;
            else
                token = new JsIdentifier(identifier);

            return literal;
        }
        
    }

    public static class CallExpressionUtils
    {
        private const char WhitespaceArgument = ':'; 
        
        public static ReadOnlySpan<char> ParseJsCallExpression(this ReadOnlySpan<char> literal, out JsCallExpression expression, bool filterExpression=false)
        {
            literal = literal.ParseIdentifier(out var token);
            
            if (!(token is JsIdentifier identifier))
                throw new SyntaxErrorException($"Expected identifier but instead found {token.DebugToken()}");

            literal = literal.AdvancePastWhitespace();

            if (literal.FirstCharEquals(WhitespaceArgument))
            {
                literal = literal.Advance(1);
                literal = literal.ParseWhitespaceArgument(out var argument);
                expression = new JsCallExpression(identifier, argument);
                return literal;
            }
            
            if (literal.StartsWith("=>"))
            {
                literal = literal.ParseArrowExpressionBody(new[]{ new JsIdentifier("it") }, out var arrowExpr);
                expression = new JsCallExpression(identifier, arrowExpr);
                return literal;
            }

            if (!literal.FirstCharEquals('('))
            {
                expression = new JsCallExpression(identifier);
                return literal;
            }
            
            literal = literal.Advance(1);

            literal = literal.ParseArguments(out var args, termination: ')');
            
            expression = new JsCallExpression(identifier, args.ToArray());
            return literal;
        }

        internal static ReadOnlySpan<char> ParseWhitespaceArgument(this ReadOnlySpan<char> literal, out JsToken argument)
        {
            // replace everything after ':' up till new line and rewrite as single string to method
            var endStringPos = literal.IndexOf("\n");
            var endStatementPos = literal.IndexOf("}}");
    
            if (endStringPos == -1 || (endStatementPos != -1 && endStatementPos < endStringPos))
                endStringPos = endStatementPos;
                            
            if (endStringPos == -1)
                throw new SyntaxErrorException($"Whitespace sensitive syntax did not find a '\\n' new line to mark the end of the statement, near {literal.DebugLiteral()}");

            var originalArg = literal.Slice(0, endStringPos).Trim().ToString();
            var rewrittenArgs = originalArg.Replace("{","{{").Replace("}","}}");
            var strArg = new JsLiteral(rewrittenArgs);

            argument = strArg;
            return literal.Slice(endStringPos);
        }

        internal static ReadOnlySpan<char> ParseArguments(this ReadOnlySpan<char> literal, out List<JsToken> arguments, char termination)
        {
            arguments = new List<JsToken>();

            while (!literal.IsNullOrEmpty())
            {
                JsToken listValue;
                
                literal = literal.AdvancePastWhitespace();
                if (literal[0] == termination)
                {
                    literal = literal.Advance(1);
                    break;
                }

                if (literal.StartsWith("..."))
                {
                    literal = literal.Advance(3);
                    literal = literal.ParseJsExpression(out listValue);
                    if (!(listValue is JsIdentifier) && !(listValue is JsArrayExpression)) 
                        throw new SyntaxErrorException($"Spread operator expected array but instead found {listValue.DebugToken()}");
                    
                    listValue = new JsSpreadElement(listValue);
                }
                else
                {
                    literal = literal.ParseJsExpression(out listValue);
                }

                arguments.Add(listValue);

                literal = literal.AdvancePastWhitespace();
                if (literal.IsNullOrEmpty())
                    break;
                    
                if (literal[0] == termination)
                {
                    literal = literal.Advance(1);
                    break;
                }

                literal = literal.AdvancePastWhitespace();
                var c = literal.SafeGetChar(0);
                if (c.IsEnd() || c == termination)
                {
                    literal = literal.Advance(1);
                    break;
                }
                    
                if (c != ',')
                    throw new SyntaxErrorException($"Unterminated arguments expression near: {literal.DebugLiteral()}");
                    
                literal = literal.Advance(1);
                literal = literal.AdvancePastWhitespace();
            }

            literal = literal.AdvancePastWhitespace();

            return literal;
        }
    }
}