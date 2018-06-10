using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Text.Json;
#if NETSTANDARD2_0
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
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

        public abstract object Evaluate(TemplateScopeContext scope);

        /// <summary>
        /// Handle sync/async results if the result can be a Task  
        /// </summary>
        public virtual Task<object> EvaluateAsync(TemplateScopeContext scope)
        {
            var result = Evaluate(scope);
            if (result is Task<object> taskObj)
                return taskObj;
            if (result is Task task)
                return task.GetResult().InTask();
            return result.InTask();
        }

        public static object UnwrapValue(JsToken token)
        {
            if (token is JsLiteral literal)
                return literal.Value;
            return null;
        }
    }

    public abstract class JsExpression : JsToken
    {
        public abstract Dictionary<string, object> ToJsAst();

        public virtual string ToJsAstType() => GetType().ToJsAstType();
    }
    
    public class JsNull : JsToken
    {
        public const string String = "null";
        
        private JsNull() {} //this is the only one
        public static JsNull Value = new JsNull();
        public override string ToRawString() => String;

        public override object Evaluate(TemplateScopeContext scope) => null;
    }

    public class JsIdentifier : JsExpression
    {
        public StringSegment Name { get; }

        private string nameString;
        public string NameString => nameString ?? (nameString = Name.HasValue ? Name.Value : null);

        public JsIdentifier(string name) => Name = name.ToStringSegment();
        public JsIdentifier(StringSegment name) => Name = name;
        public override string ToRawString() => ":" + Name;
        
        public override object Evaluate(TemplateScopeContext scope)
        {
            var ret = scope.PageResult.GetValue(NameString, scope);
            return ret;
        }

        protected bool Equals(JsIdentifier other) => string.Equals(Name, other.Name);

        public override Dictionary<string, object> ToJsAst() => new Dictionary<string, object> {
            ["type"] = ToJsAstType(),
            ["name"] = NameString,
        };

        public override int GetHashCode() => Name.GetHashCode();

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsIdentifier) obj);
        }

        public override string ToString() => ToRawString();
    }

    public class JsLiteral : JsExpression
    {
        public static JsLiteral True = new JsLiteral(true);
        public static JsLiteral False = new JsLiteral(false);
        
        public object Value { get; }
        public JsLiteral(object value) => Value = value;
        public override string ToRawString() => JsonValue(Value);

        public override int GetHashCode() => (Value != null ? Value.GetHashCode() : 0);
        protected bool Equals(JsLiteral other) => Equals(Value, other.Value);
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == this.GetType() && Equals((JsLiteral) obj);
        }

        public override string ToString() => ToRawString();

        public override object Evaluate(TemplateScopeContext scope) => Value;

        public override Dictionary<string, object> ToJsAst() => new Dictionary<string, object> {
            ["type"] = ToJsAstType(),
            ["value"] = Value,
            ["raw"] = JsonValue(Value),
        };
    }

    public class JsTemplateLiteral : JsExpression
    {
        public JsTemplateElement[] Quasis { get; }
        public JsToken[] Expressions { get; }
        
        public JsTemplateLiteral(string cooked) 
            : this(new []{ new JsTemplateElement(cooked, cooked, tail:true) }){}

        public JsTemplateLiteral(JsTemplateElement[] quasis=null, JsToken[] expressions=null)
        {
            Quasis = quasis ?? TypeConstants<JsTemplateElement>.EmptyArray;
            Expressions = expressions ?? TypeConstants<JsToken>.EmptyArray;
        }

        public override string ToRawString()
        {
            var sb = StringBuilderCache.Allocate();

            sb.Append("`");

            for (int i = 0; i < Quasis.Length; i++)
            {
                var quasi = Quasis[i];
                sb.Append(quasi.Value.Raw);
                if (quasi.Tail)
                    break;

                var expr = Expressions[i];
                sb.Append("${");
                sb.Append(expr.ToRawString());
                sb.Append("}");
            }

            sb.Append("`");

            var ret = StringBuilderCache.ReturnAndFree(sb);
            return ret;
        }

        public override object Evaluate(TemplateScopeContext scope)
        {
            var sb = StringBuilderCache.Allocate();
            
            for (int i = 0; i < Quasis.Length; i++)
            {
                var quasi = Quasis[i];
                sb.Append(quasi.Value.Cooked);
                if (quasi.Tail)
                    break;

                var expr = Expressions[i];
                var value = expr.Evaluate(scope);
                sb.Append(value);
            }

            var ret = StringBuilderCache.ReturnAndFree(sb);
            return ret;
        }

        public override Dictionary<string, object> ToJsAst()
        {
            var to = new Dictionary<string, object> {
                ["type"] = ToJsAstType(),
            };

            var quasiType = typeof(JsTemplateElement).ToJsAstType();
            var quasis = new List<object>();
            foreach (var quasi in Quasis)
            {
                quasis.Add(new Dictionary<string, object> {
                    ["type"] = quasiType,
                    ["value"] = new Dictionary<string, object> {
                        ["raw"] = quasi.Value.Raw,
                        ["cooked"] = quasi.Value.Cooked,
                    },
                    ["tail"] = quasi.Tail,
                });
            }
            to["quasis"] = quasis;

            var expressions = new List<object>();
            foreach (var expression in Expressions)
            {
                expressions.Add(expression.ToJsAst());
            }
            to["expressions"] = expressions;

            return to;
        }

        protected bool Equals(JsTemplateLiteral other)
        {
            return Quasis.EquivalentTo(other.Quasis) && 
                   Expressions.EquivalentTo(other.Expressions);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsTemplateLiteral) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Quasis != null ? Quasis.GetHashCode() : 0) * 397) ^ (Expressions != null ? Expressions.GetHashCode() : 0);
            }
        }

        public override string ToString() => ToRawString();
    }

    public class JsTemplateElement
    {
        public JsTemplateElementValue Value { get; }
        public bool Tail { get; }
        
        public JsTemplateElement(string raw, string cooked, bool tail=false) : 
            this(new JsTemplateElementValue(raw, cooked), tail){}
        
        public JsTemplateElement(JsTemplateElementValue value, bool tail)
        {
            Value = value;
            Tail = tail;
        }

        protected bool Equals(JsTemplateElement other)
        {
            return Equals(Value, other.Value) && Tail == other.Tail;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsTemplateElement) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Value != null ? Value.GetHashCode() : 0) * 397) ^ Tail.GetHashCode();
            }
        }
    }

    public class JsTemplateElementValue
    {
        public string Raw { get; }
        public string Cooked { get; }
        
        public JsTemplateElementValue(string raw, string cooked)
        {
            Raw = raw;
            Cooked = cooked;
        }

        protected bool Equals(JsTemplateElementValue other)
        {
            return Raw.Equals(other.Raw) && Cooked.Equals(other.Cooked);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsTemplateElementValue) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Raw.GetHashCode() * 397) ^ Cooked.GetHashCode();
            }
        }
    }

    public class JsArrayExpression : JsExpression
    {
        public JsToken[] Elements { get; }

        public JsArrayExpression(params JsToken[] elements) => Elements = elements.ToArray();
        public JsArrayExpression(IEnumerable<JsToken> elements) : this(elements.ToArray()) {}

        public override object Evaluate(TemplateScopeContext scope)
        {
            var to = new List<object>();
            foreach (var element in Elements)
            {
                if (element is JsSpreadElement spread)
                {
                    var arr = spread.Argument.Evaluate(scope) as IEnumerable;
                    if (arr == null)
                        continue;

                    foreach (var value in arr)
                    {
                        to.Add(value);
                    }
                }
                else
                {
                    var value = element.Evaluate(scope);
                    to.Add(value);
                }
            }
            return to;
        }

        public override string ToRawString()
        {
            var sb = StringBuilderCache.Allocate();
            sb.Append("[");
            for (var i = 0; i < Elements.Length; i++)
            {
                if (i > 0) 
                    sb.Append(",");
                
                var element = Elements[i];
                sb.Append(element.ToRawString());
            }
            sb.Append("]");
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public override Dictionary<string, object> ToJsAst()
        {
            var elements = new List<object>();
            var to = new Dictionary<string, object>
            {
                ["type"] = ToJsAstType(),
                ["elements"] = elements
            };

            foreach (var element in Elements)
            {
                elements.Add(element.ToJsAst());
            }

            return to;
        }

        protected bool Equals(JsArrayExpression other)
        {
            return Elements.EquivalentTo(other.Elements);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsArrayExpression) obj);
        }

        public override int GetHashCode()
        {
            return (Elements != null ? Elements.GetHashCode() : 0);
        }
    }

    public class JsObjectExpression : JsExpression
    {
        public JsProperty[] Properties { get; }

        public JsObjectExpression(params JsProperty[] properties) => Properties = properties;
        public JsObjectExpression(IEnumerable<JsProperty> properties) : this(properties.ToArray()) {}

        public static string GetKey(JsToken token)
        {
            if (token is JsLiteral literalKey)
                return literalKey.Value.ToString();
            if (token is JsIdentifier identifierKey)
                return identifierKey.NameString;
            
            throw new SyntaxErrorException($"Invalid Key. Expected a Literal or Identifier but was {token.DebugToken()}");
        }

        public override object Evaluate(TemplateScopeContext scope)
        {
            var to = new Dictionary<string, object>();
            foreach (var prop in Properties)
            {
                if (prop.Key == null)
                {
                    if (prop.Value is JsSpreadElement spread)
                    {
                        var value = spread.Argument.Evaluate(scope);
                        var obj = value.ToObjectDictionary();
                        if (obj != null)
                        {
                            foreach (var entry in obj)
                            {
                                to[entry.Key] = entry.Value;
                            }
                        }
                    }
                    else
                    {
                        throw new NotSupportedException("Object Expressions does not have a key");
                    }
                }
                else
                {
                    var keyString = GetKey(prop.Key);
                    var value = prop.Value.Evaluate(scope);
                    to[keyString] = value;
                }
            }
            return to;
        }

        public override string ToRawString()
        {
            var sb = StringBuilderCache.Allocate();
            sb.Append("{");
            for (var i = 0; i < Properties.Length; i++)
            {
                if (i > 0) 
                    sb.Append(",");
                
                var prop = Properties[i];
                if (prop.Key != null)
                {
                    sb.Append(prop.Key.ToRawString());
                    if (!prop.Shorthand)
                    {
                        sb.Append(":");
                        sb.Append(prop.Value.ToRawString());
                    }
                }
                else //.... spread operator
                {
                    sb.Append(prop.Value.ToRawString());
                }
            }
            sb.Append("}");
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public override Dictionary<string, object> ToJsAst()
        {
            var properties = new List<object>();
            var to = new Dictionary<string, object>
            {
                ["type"] = ToJsAstType(),
                ["properties"] = properties
            };

            var propType = typeof(JsProperty).ToJsAstType();
            foreach (var prop in Properties)
            {
                properties.Add(new Dictionary<string, object> {
                    ["type"] = propType,
                    ["key"] = prop.Key?.ToJsAst(), 
                    ["computed"] = false, //syntax not supported: { ["a" + 1]: 2 }
                    ["value"] = prop.Value.ToJsAst(), 
                    ["kind"] = "init",
                    ["method"] = false,
                    ["shorthand"] = prop.Shorthand,
                });
            }

            return to;
        }

        protected bool Equals(JsObjectExpression other)
        {
            return Properties.EquivalentTo(other.Properties);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsObjectExpression) obj);
        }

        public override int GetHashCode()
        {
            return (Properties != null ? Properties.GetHashCode() : 0);
        }
    }

    public class JsProperty
    {
        public JsToken Key { get; }
        public JsToken Value { get; }
        public bool Shorthand { get; }

        public JsProperty(JsToken key, JsToken value) : this(key, value, shorthand:false){}
        public JsProperty(JsToken key, JsToken value, bool shorthand)
        {
            Key = key;
            Value = value;
            Shorthand = shorthand;
        }

        protected bool Equals(JsProperty other)
        {
            return Equals(Key, other.Key) && 
                   Equals(Value, other.Value) && 
                   Shorthand == other.Shorthand;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsProperty) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Key != null ? Key.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Value != null ? Value.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Shorthand.GetHashCode();
                return hashCode;
            }
        }
    }

    public class JsSpreadElement : JsExpression
    {
        public JsToken Argument { get; }
        public JsSpreadElement(JsToken argument)
        {
            Argument = argument;
        }

        public override object Evaluate(TemplateScopeContext scope)
        {
            return Argument.Evaluate(scope);
        }

        public override string ToRawString()
        {
            return "..." + Argument.ToRawString();
        }

        public override Dictionary<string, object> ToJsAst() => new Dictionary<string, object>
        {
            ["type"] = ToJsAstType(),
            ["argument"] = Argument.ToJsAst()
        };

        protected bool Equals(JsSpreadElement other)
        {
            return Equals(Argument, other.Argument);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsSpreadElement) obj);
        }

        public override int GetHashCode()
        {
            return (Argument != null ? Argument.GetHashCode() : 0);
        }
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
        public static StringSegment AdvancePastWhitespace(this StringSegment literal)
        {
            var i = 0;
            while (i < literal.Length && literal.GetChar(i).IsWhiteSpace())
                i++;

            return i == 0 ? literal : literal.Subsegment(i < literal.Length ? i : literal.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FirstCharEquals(this StringSegment literal, char c) => 
            !literal.IsNullOrEmpty() && literal.GetChar(0) == c;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char SafeGetChar(this StringSegment literal, int index) =>
            index >= 0 && index < literal.Length ? literal.GetChar(index) : default(char);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnd(this char c) => c == default(char);

        // Remove `Js` prefix
        public static string ToJsAstType(this Type type) => type.Name.Substring(2);

        public static Dictionary<string, object> ToJsAst(this JsToken token) => token is JsExpression expression
            ? expression.ToJsAst()
            : throw new NotSupportedException(token.GetType().Name + " is not a JsExpression");

        internal static string DebugFirstChar(this StringSegment literal)
        {
            return literal.IsNullOrEmpty()
                ? "<end>"
                : $"'{literal.GetChar(0)}'";
        }

        internal static string DebugChar(this char c) => c == 0 ? "'<end>'" : $"'{c}'";

        internal static string DebugToken(this JsToken token) => $"'{token}'";

        internal static string DebugLiteral(this StringSegment literal) => $"'{literal.SubstringWithEllipsis(0, 50)}'";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static string CookRawString(this StringSegment str, char quoteChar) =>
            JsonTypeSerializer.UnescapeJsString(str, quoteChar).Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static StringSegment TrimFirstNewLine(this StringSegment literal) => literal.StartsWith("\r\n")
            ? literal.Advance(2)
            : (literal.StartsWith("\n") ? literal.Advance(1) : literal);

        public static bool EvaluateToBool(this JsToken token, TemplateScopeContext scope)
        {
            var ret = token.Evaluate(scope);
            if (ret is bool b)
                return b;

            return !TemplateDefaultFilters.isFalsy(ret);
        }

        /// <summary>
        /// Handle sync/async results if the result can be a Task  
        /// </summary>
        public static async Task<bool> EvaluateToBoolAsync(this JsToken token, TemplateScopeContext scope)
        {
            var ret = await token.EvaluateAsync(scope);
            if (ret is bool b)
                return b;

            return !TemplateDefaultFilters.isFalsy(ret);
        }

        public static StringSegment AdvancePastChar(this StringSegment literal, char delim)
        {
            var i = 0;
            var c = (char) 0;
            while (i < literal.Length && (c = literal.GetChar(i)) != delim)
                i++;

            if (c == delim)
                return literal.Subsegment(i + 1);

            return i == 0 ? literal : literal.Subsegment(i < literal.Length ? i : literal.Length);
        }
        
        public static StringSegment ParseJsToken(this StringSegment literal, out JsToken token) => ParseJsToken(literal, out token, false);
        public static StringSegment ParseJsToken(this StringSegment literal, out JsToken token, bool filterExpression)
        {
            literal = literal.AdvancePastWhitespace();

            if (literal.IsNullOrEmpty())
            {
                token = null;
                return literal;
            }
            
            var c = literal.GetChar(0);
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
                
                throw new SyntaxErrorException($"Expected ')' but instead found {literal.DebugFirstChar()} near: {literal.DebugLiteral()}");
            }

            token = null;
            c = (char) 0;

            if (literal.IsNullOrEmpty())
                return TypeConstants.EmptyStringSegment;

            var i = 0;
            literal = literal.AdvancePastWhitespace();

            var firstChar = literal.GetChar(0);
            if (firstChar == '\'' || firstChar == '"' || firstChar == '`' || firstChar == '′')
            {
                var quoteChar = firstChar;
                i = 1;
                var hasEscapeChar = false;
                
                while (i < literal.Length)
                {
                    c = literal.GetChar(i);
                    if (c == quoteChar)
                    {
                        if (literal.SafeGetChar(i - 1) != '\\' || literal.SafeGetChar(i - 2) == '\\')
                            break;
                    }
                    
                    i++;
                    if (!hasEscapeChar)
                        hasEscapeChar = c == '\\';
                }

                if (i >= literal.Length || literal.GetChar(i) != quoteChar)
                    throw new SyntaxErrorException($"Unterminated string literal: {literal}");

                var rawString = literal.Subsegment(1, i - 1);

                if (quoteChar == '`')
                {
                    token = ParseJsTemplateLiteral(rawString);
                }
                else if (hasEscapeChar)
                {
                    //All other quoted strings use unescaped strings  
                    var sb = StringBuilderCache.Allocate();
                    for (var j = 0; j < rawString.Length; j++)
                    {
                        // strip the back-slash used to escape quote char in strings
                        var ch = rawString.GetChar(j);
                        if (ch != '\\' || (j + 1 >= rawString.Length || rawString.GetChar(j + 1) != quoteChar))
                            sb.Append(ch);
                    }
                    token = new JsLiteral(StringBuilderCache.ReturnAndFree(sb));
                }
                else
                {
                    token = new JsLiteral(rawString.Value);                    
                }
                
                return literal.Advance(i + 1);
            }

            if (firstChar >= '0' && firstChar <= '9')
            {
                i = 1;
                var hasExponent = false;
                var hasDecimal = false;

                while (i < literal.Length && IsNumericChar(c = literal.GetChar(i)) ||
                       (hasExponent = (c == 'e' || c == 'E')))
                {
                    if (c == '.')
                        hasDecimal = true;

                    i++;

                    if (hasExponent)
                    {
                        i += 2; // [e+1]0

                        while (i < literal.Length && IsNumericChar(literal.GetChar(i)))
                            i++;

                        break;
                    }
                }

                var numLiteral = literal.Subsegment(0, i);

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
                    if (literal.GetChar(0) == '}')
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
    
                        if (!(mapKeyToken is JsLiteral) && !(mapKeyToken is JsTemplateLiteral) && !(mapKeyToken is JsIdentifier)) 
                            throw new SyntaxErrorException($"{mapKeyToken.DebugToken()} is not a valid Object key, expected literal or identifier.");
    
                        bool shorthand = false;
    
                        literal = literal.AdvancePastWhitespace();
                        if (literal.Length > 0 && literal.GetChar(0) == ':')
                        {
                            literal = literal.Advance(1);
                            literal = literal.ParseJsExpression(out mapValueToken);
    
                        }
                        else 
                        {
                            shorthand = true;
                            if (literal.Length == 0 || (c = literal.GetChar(0)) != ',' && c != '}')
                                throw new SyntaxErrorException($"Unterminated object literal near: {literal.DebugLiteral()}");
                                
                            mapValueToken = mapKeyToken;
                        }
                        
                        props.Add(new JsProperty(mapKeyToken, mapValueToken, shorthand));
                    }

                    literal = literal.AdvancePastWhitespace();
                    if (literal.IsNullOrEmpty())
                        break;

                    if (literal.GetChar(0) == '}')
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
            if (unaryOp != null && literal.SafeGetChar(1).IsValidVarNameChar())
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

        private static JsToken ParseJsTemplateLiteral(StringSegment literal)
        {
            var quasis = new List<JsTemplateElement>();
            var expressions = new List<JsToken>();
            var lastPos = 0;

            for (var i = 0; i < literal.Length; i++)
            {
                var c = literal.GetChar(i);
                if (c != '$' || literal.SafeGetChar(i-1) == '\\' || literal.SafeGetChar(i+1) != '{')
                    continue;

                var lastChunk = literal.Subsegment(lastPos, i - lastPos);
                quasis.Add(new JsTemplateElement(
                    new JsTemplateElementValue(lastChunk.Value, lastChunk.CookRawString('`')), 
                    tail:false));

                var exprStart = literal.Subsegment(i + 2);
                var afterExpr = exprStart.ParseJsExpression(out var expr);
                afterExpr = afterExpr.AdvancePastWhitespace();
                
                if (!afterExpr.FirstCharEquals('}'))
                    throw new SyntaxErrorException($"Expected end of template literal expression '}}' but was instead {literal.DebugFirstChar()}");
                afterExpr = afterExpr.Advance(1);
                
                expressions.Add(expr);

                i = lastPos = literal.Length - afterExpr.Length;
            }

            var endChunk = literal.Subsegment(lastPos);

            quasis.Add(new JsTemplateElement(
                new JsTemplateElementValue(endChunk.Value, endChunk.CookRawString('`')), 
                tail:true));
            
            return new JsTemplateLiteral(quasis.ToArray(), expressions.ToArray());
        }

        internal static StringSegment ParseJsMemberExpression(this StringSegment literal, ref JsToken node, bool filterExpression)
        {
            literal = literal.AdvancePastWhitespace();

            if (literal.IsNullOrEmpty())
                return literal;
            
            var c = literal.GetChar(0);

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
                else if (filterExpression && c == ':')
                {
                    literal = literal.ParseWhitespaceArgument(out var argument);
                    node = new JsCallExpression(node, argument);
                    return literal;
                }

                literal = literal.AdvancePastWhitespace();
                    
                if (literal.IsNullOrEmpty())
                    break;

                c = literal.GetChar(0);
            }

            return literal;
        }

        internal static StringSegment ParseJsBinaryOperator(this StringSegment literal, out JsBinaryOperator op)
        {
            literal = literal.AdvancePastWhitespace();
            op = null;
            
            if (literal.IsNullOrEmpty())
                return literal;
            
            var firstChar = literal.GetChar(0);
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

        internal static StringSegment ParseVarName(this StringSegment literal, out StringSegment varName)
        {
            literal = literal.AdvancePastWhitespace();

            var c = literal.SafeGetChar(0);
            if (!c.IsValidVarNameChar())
                throw new SyntaxErrorException($"Expected start of identifier but was {c.DebugChar()} near: {literal.DebugLiteral()}");

            var i = 1;
            
            while (i < literal.Length)
            {
                c = literal.GetChar(i);

                if (IsValidVarNameChar(c))
                    i++;
                else
                    break;
            }

            varName = literal.Subsegment(0, i).TrimEnd();
            literal = literal.Advance(i);

            return literal;
            
        }

        internal static StringSegment ParseIdentifier(this StringSegment literal, out JsToken token)
        {
            literal = literal.ParseVarName(out var identifier);
            
            if (identifier.Equals("true"))
                token = JsLiteral.True;
            else if (identifier.Equals("false"))
                token = JsLiteral.False;
            else if (identifier.Equals("null"))
                token = JsNull.Value;
            else if (identifier.Equals("and"))
                token = JsAnd.Operator;
            else if (identifier.Equals("or"))
                token = JsOr.Operator;
            else
                token = new JsIdentifier(identifier);

            return literal;
        }
        
    }

    public static class CallExpressionUtils
    {
        private const char WhitespaceArgument = ':'; 
        
        public static StringSegment ParseJsCallExpression(this StringSegment literal, out JsCallExpression expression, bool filterExpression=false)
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

        internal static StringSegment ParseWhitespaceArgument(this StringSegment literal, out JsToken argument)
        {
            // replace everything after ':' up till new line and rewrite as single string to method
            var endStringPos = literal.IndexOf("\n");
            var endStatementPos = literal.IndexOf("}}");
    
            if (endStringPos == -1 || (endStatementPos != -1 && endStatementPos < endStringPos))
                endStringPos = endStatementPos;
                            
            if (endStringPos == -1)
                throw new SyntaxErrorException($"Whitespace sensitive syntax did not find a '\\n' new line to mark the end of the statement, near {literal.DebugLiteral()}");

            var originalArg = literal.Subsegment(0, endStringPos).Trim().ToString();
            var rewrittenArgs = originalArg.Replace("{","{{").Replace("}","}}");
            var strArg = new JsLiteral(rewrittenArgs);

            argument = strArg;
            return literal.Subsegment(endStringPos);
        }

        internal static StringSegment ParseArguments(this StringSegment literal, out List<JsToken> arguments, char termination)
        {
            arguments = new List<JsToken>();

            while (!literal.IsNullOrEmpty())
            {
                JsToken listValue;
                
                literal = literal.AdvancePastWhitespace();
                if (literal.GetChar(0) == termination)
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
                    
                if (literal.GetChar(0) == termination)
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