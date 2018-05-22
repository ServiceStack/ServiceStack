using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using ServiceStack.Text;

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

        public static JsToken Create(string js) => Create(js.ToStringSegment());

        public static JsToken Create(StringSegment js)
        {
            js.ParseJsExpression(out var token);
            
            return token;

//            if (token != null)
//                return token;
//
//            if (value is string s)
//                return new JsString(s);
//            if (value is int i)
//                return new JsNumber(i);
//            if (value is long l)
//                return new JsNumber(l);
//            if (value is double d)
//                return new JsNumber(d);
//            if (value is List<object> list)
//                return new JsArray(list);
//            if (value is Dictionary<string,object> map)
//                return new JsObject(map);
//            if (value is null || value == JsNull.Value)
//                return JsNull.Value;
//
//            throw new NotSupportedException($"Unknown value JsToken '{value}'");
        }

        public override string ToString() => ToRawString();

        public abstract object Evaluate(TemplateScopeContext scope);

        public static object UnwrapValue(JsToken token)
        {
            if (token is JsLiteral literal)
                return literal.Value;
            if (token is JsLiteralExpression literalExp)
                return UnwrapValue(literalExp.Value);
            return null;
        }
    }
    
    public class JsNull : JsToken
    {
        public const string String = "null";
        
        private JsNull() {} //this is the only one
        public static JsNull Value = new JsNull();
        public override string ToRawString() => String;

        public override object Evaluate(TemplateScopeContext scope) => null;
    }

    public class JsLiteral : JsToken
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
    }
    
    public class JsLiteralExpression : JsExpression
    {
        public JsToken Value { get; }
        public override string ToRawString() => JsonValue(Value);
        public JsLiteralExpression(JsToken target)
        {
            Value = target;
        }

        public override object Evaluate(TemplateScopeContext scope)
        {
            var result = scope.EvaluateToken(Value);
            return result;
        }

        protected bool Equals(JsLiteralExpression other)
        {
            return Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsLiteralExpression) obj);
        }

        public override int GetHashCode()
        {
            return (Value != null ? Value.GetHashCode() : 0);
        }
    }

    public class JsIdentifier : JsToken
    {
        public StringSegment Name { get; }

        private string nameString;
        public string NameString => nameString ?? (nameString = Name.HasValue ? Name.Value : null);

        public JsIdentifier(string name) => Name = name.ToStringSegment();
        public JsIdentifier(StringSegment name) => Name = name;
        public override string ToRawString() => ":" + Name;

        protected bool Equals(JsIdentifier other) => string.Equals(Name, other.Name);
        public override int GetHashCode() => Name.GetHashCode();

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsIdentifier) obj);
        }

        public override string ToString() => ToRawString();
        
        public override object Evaluate(TemplateScopeContext scope)
        {
            var ret = scope.PageResult.GetValue(NameString, scope);
            return ret;
        }
    }

    public class JsArrayExpression : JsToken
    {
        public JsToken[] Elements { get; }

        public JsArrayExpression(params JsToken[] elements) => Elements = elements.ToArray();
        public JsArrayExpression(IEnumerable<JsToken> elements) : this(elements.ToArray()) {}

        public override object Evaluate(TemplateScopeContext scope)
        {
            var to = new List<object>();
            foreach (var element in Elements)
            {
                var value = element.Evaluate(scope);
                to.Add(value);
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

    public class JsObjectExpression : JsToken
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
            if (token is JsLiteralExpression literalExp)
                return GetKey(literalExp.Value);
            
            throw new ArgumentException($"Invalid Key. Expected a Literal or Identifier but was '{token}'");
        }

        public override object Evaluate(TemplateScopeContext scope)
        {
            var to = new Dictionary<string, object>();
            foreach (var prop in Properties)
            {
                var keyString = GetKey(prop.Key);
                var value = prop.Value.Evaluate(scope);
                to[keyString] = value;
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
                sb.Append(prop.Key.ToRawString());
                if (!prop.Shorthand)
                {
                    sb.Append(":");
                    sb.Append(prop.Value.ToRawString());
                }
            }
            sb.Append("}");
            return StringBuilderCache.ReturnAndFree(sb);
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

        public JsProperty(JsToken key, JsToken value) : this(key, value, false){}
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

    public abstract class JsOperator : JsToken
    {
        public abstract string Token { get; }
        public override string ToRawString() => Token;
        public override object Evaluate(TemplateScopeContext scope) => this;
    }

    public abstract class JsBinaryOperator : JsOperator
    {
        public abstract object Evaluate(object lhs, object rhs);
    }

    public abstract class JsUnaryOperator : JsOperator
    {
        public abstract object Evaluate(object target);
        public static JsUnaryOperator GetUnaryOperator(JsOperator op) => 
            (JsUnaryOperator) (
                op == JsSubtraction.Operator 
                ? JsMinus.Operator 
                : op == JsNot.Operator
                ? op : null);
    }
    public abstract class JsLogicOperator : JsBinaryOperator
    {
        public abstract bool Test(object lhs, object rhs);
        public override object Evaluate(object lhs, object rhs) => Test(lhs, rhs);
    }
    public class JsGreaterThan : JsLogicOperator
    {
        public static JsGreaterThan Operator = new JsGreaterThan();
        private JsGreaterThan(){}
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.Instance.greaterThan(lhs, rhs);
        public override string Token => ">";
    }
    public class JsGreaterThanEqual : JsLogicOperator
    {
        public static JsGreaterThanEqual Operator = new JsGreaterThanEqual();
        private JsGreaterThanEqual(){}
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.Instance.greaterThanEqual(lhs, rhs);
        public override string Token => ">=";
    }
    public class JsLessThanEqual : JsLogicOperator
    {
        public static JsLessThanEqual Operator = new JsLessThanEqual();
        private JsLessThanEqual(){}
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.Instance.lessThanEqual(lhs, rhs);
        public override string Token => "<=";
    }
    public class JsLessThan : JsLogicOperator
    {
        public static JsLessThan Operator = new JsLessThan();
        private JsLessThan(){}
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.Instance.lessThan(lhs, rhs);
        public override string Token => "<";
    }
    public class JsEquals : JsLogicOperator
    {
        public static JsEquals Operator = new JsEquals();
        private JsEquals(){}
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.Instance.equals(lhs, rhs);
        public override string Token => "==";
    }
    public class JsNotEquals : JsLogicOperator
    {
        public static JsNotEquals Operator = new JsNotEquals();
        private JsNotEquals(){}
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.Instance.notEquals(lhs, rhs);
        public override string Token => "!=";
    }
    public class JsStrictEquals : JsLogicOperator
    {
        public static JsStrictEquals Operator = new JsStrictEquals();
        private JsStrictEquals(){}
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.Instance.equals(lhs, rhs);
        public override string Token => "===";
    }
    public class JsStrictNotEquals : JsLogicOperator
    {
        public static JsStrictNotEquals Operator = new JsStrictNotEquals();
        private JsStrictNotEquals(){}
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.Instance.notEquals(lhs, rhs);
        public override string Token => "!==";
    }
    public class JsAssignment : JsBinaryOperator
    {
        public static JsAssignment Operator = new JsAssignment();
        private JsAssignment(){}
        public override string Token => "=";
        public override object Evaluate(object lhs, object rhs) => rhs;
    }
    public class JsOr : JsLogicOperator
    {
        public static JsOr Operator = new JsOr();
        private JsOr(){}
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.isTrue(lhs) || TemplateDefaultFilters.isTrue(rhs);
        public override string Token => "||";
    }
    public class JsAnd : JsLogicOperator
    {
        public static JsAnd Operator = new JsAnd();
        private JsAnd(){}
        public override bool Test(object lhs, object rhs) => TemplateDefaultFilters.isTrue(lhs) && TemplateDefaultFilters.isTrue(rhs);
        public override string Token => "&&";
    }
    public class JsNot : JsUnaryOperator
    {
        public static JsNot Operator = new JsNot();
        private JsNot(){}
        public override string Token => "!";
        public override object Evaluate(object target) => !TemplateDefaultFilters.isTrue(target);
    }
    public class JsBitwiseAnd : JsBinaryOperator
    {
        public static JsBitwiseAnd Operator = new JsBitwiseAnd();
        private JsBitwiseAnd(){}
        public override string Token => "&";
        
        public override object Evaluate(object lhs, object rhs) => 
            DynamicNumber.GetNumber(lhs, rhs).bitwiseAnd(lhs, rhs);
    }
    public class JsBitwiseOr : JsBinaryOperator
    {
        public static JsBitwiseOr Operator = new JsBitwiseOr();
        private JsBitwiseOr(){}
        public override string Token => "|";
        
        public override object Evaluate(object lhs, object rhs) => 
            DynamicNumber.GetNumber(lhs, rhs).bitwiseOr(lhs, rhs);
    }
    public class JsBitwiseXOr : JsBinaryOperator
    {
        public static JsBitwiseXOr Operator = new JsBitwiseXOr();
        private JsBitwiseXOr(){}
        public override string Token => "^";
        
        public override object Evaluate(object lhs, object rhs) => 
            DynamicNumber.GetNumber(lhs, rhs).bitwiseXOr(lhs, rhs);
    }
    public class JsBitwiseLeftShift : JsBinaryOperator
    {
        public static JsBitwiseLeftShift Operator = new JsBitwiseLeftShift();
        private JsBitwiseLeftShift(){}
        public override string Token => "<<";
        
        public override object Evaluate(object lhs, object rhs) => 
            DynamicNumber.GetNumber(lhs, rhs).bitwiseLeftShift(lhs, rhs);
    }
    public class JsBitwiseRightShift : JsBinaryOperator
    {
        public static JsBitwiseRightShift Operator = new JsBitwiseRightShift();
        private JsBitwiseRightShift(){}
        public override string Token => ">>";
        
        public override object Evaluate(object lhs, object rhs) => 
            DynamicNumber.GetNumber(lhs, rhs).bitwiseRightShift(lhs, rhs);
    }
    public class JsAddition : JsBinaryOperator
    {
        public static JsAddition Operator = new JsAddition();
        private JsAddition(){}
        public override string Token => "+";
        
        public override object Evaluate(object lhs, object rhs) => 
            DynamicNumber.GetNumber(lhs, rhs).add(lhs, rhs);
    }
    public class JsSubtraction : JsBinaryOperator
    {
        public static JsSubtraction Operator = new JsSubtraction();
        private JsSubtraction(){}
        public override string Token => "-";
        
        public override object Evaluate(object lhs, object rhs) => 
            DynamicNumber.GetNumber(lhs, rhs).sub(lhs, rhs);
    }
    public class JsMultiplication : JsBinaryOperator
    {
        public static JsMultiplication Operator = new JsMultiplication();
        private JsMultiplication(){}
        public override string Token => "*";
        
        public override object Evaluate(object lhs, object rhs) => 
            DynamicNumber.GetNumber(lhs, rhs).mul(lhs, rhs);
    }
    public class JsDivision : JsBinaryOperator
    {
        public static JsDivision Operator = new JsDivision();
        private JsDivision(){}
        public override string Token => "/";
        
        public override object Evaluate(object lhs, object rhs) => 
            DynamicNumber.GetNumber(lhs, rhs).div(lhs, rhs);
    }
    public class JsMod : JsBinaryOperator
    {
        public static JsMod Operator = new JsMod();
        private JsMod(){}
        public override string Token => "%";
        
        public override object Evaluate(object lhs, object rhs) => 
            DynamicNumber.GetNumber(lhs, rhs).mod(lhs, rhs);
    }
    public class JsMinus : JsUnaryOperator
    {
        public static JsMinus Operator = new JsMinus();
        private JsMinus(){}
        public override string Token => "-";
        public override object Evaluate(object target) => target == null 
            ? 0 
            : DynamicNumber.Multiply(target, -1).ConvertTo(target.GetType());
    }
    public class JsPlus : JsUnaryOperator
    {
        public static JsPlus Operator = new JsPlus();
        private JsPlus(){}
        public override string Token => "+";
        public override object Evaluate(object target) => target ?? 0;
    }

    public class JsArray : JsToken, IEnumerable<object>
    {
        public object[] Array { get; }
        public JsArray(IEnumerable array) => Array = array.Cast<object>().ToArray();

        public override string ToRawString()
        {
            var sb = StringBuilderCache.Allocate().Append('[');
            foreach (var item in Array)
            {
                sb.Append(JsonValue(item));
            }
            sb.Append(']');
            return StringBuilderCache.ReturnAndFree(sb);
        }

        protected bool Equals(JsArray other)
        {
            if (Array == null || other.Array == null)
                return Array == other.Array;

            if (Array.Length != other.Array.Length)
                return false;

            for (var i = 0; i < Array.Length; i++)
            {
                if (!Equals(Array[i], other.Array[i]))
                    return false;
            }
            return true;
        }

        public IEnumerator<object> GetEnumerator() => ((IEnumerable<object>)Array).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsArray) obj);
        }

        public override int GetHashCode() => (Array != null ? Array.GetHashCode() : 0);

        public override object Evaluate(TemplateScopeContext scope) => Array;
    }

    public class JsObject : JsToken, IEnumerable<KeyValuePair<string, object>>
    {
        public Dictionary<string, object> Object { get; }
        public JsObject(Dictionary<string, object> obj) => Object = obj;

        public override string ToRawString()
        {
            var sb = StringBuilderCache.Allocate().Append("{");
            foreach (var entry in Object)
            {
                sb.Append('"')
                    .Append(entry.Key)
                    .Append('"')
                    .Append(":")
                    .Append(JsonValue(entry.Value));
            }
            sb.Append("}");
            return StringBuilderCache.ReturnAndFree(sb);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => Object.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override object Evaluate(TemplateScopeContext scope) => Object;
    }

    public class JsString : JsToken
    {
        public string Value { get; }
        public JsString(string value) => Value = value;
        public override string ToRawString() => Value.EncodeJson();

        protected bool Equals(JsString other) => string.Equals(Value, other.Value);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsString) obj);
        }

        public override int GetHashCode() => (Value != null ? Value.GetHashCode() : 0);

        public override object Evaluate(TemplateScopeContext scope) => Value;
    }

    public class JsNumber : JsToken
    {
        public int IntValue =>
            intValue.GetValueOrDefault((int) longValue.GetValueOrDefault((long) doubleValue.GetValueOrDefault(0)));

        public long LongValue =>
            longValue.GetValueOrDefault((long) doubleValue.GetValueOrDefault(intValue.GetValueOrDefault(0)));

        public double DoubleValue =>
            doubleValue.GetValueOrDefault(longValue.GetValueOrDefault(intValue.GetValueOrDefault(0)));

        private int? intValue;
        private long? longValue;
        private double? doubleValue;

        public JsNumber(int intValue) => this.intValue = intValue;
        public JsNumber(long longValue) => this.longValue = longValue;
        public JsNumber(double doubleValue) => this.doubleValue = doubleValue;

        public JsNumber(object numValue)
        {
            if (numValue is int i)
                intValue = i;
            else if (numValue is long l)
                longValue = l;
            else if (numValue is double d)
                doubleValue = d;
        }

        public override string ToRawString() =>
            intValue?.ToString() ?? longValue?.ToString() ?? doubleValue?.ToString(CultureInfo.InvariantCulture) ?? "0";

        public override object Evaluate(TemplateScopeContext scope)
        {
            if (intValue != null)
                return IntValue;
            if (longValue != null)
                return longValue;
            return doubleValue;
        }
    }

    public static class JsTokenUtils
    {
        private static readonly byte[] ValidNumericChars;
        private static readonly byte[] ValidVarNameChars;
        private static readonly byte[] OperatorChars;
        private const byte True = 1;

        public static readonly Dictionary<string, int> OperatorPrecedence = new Dictionary<string, int> {
            {")", 0},
            {";", 0},
            {",", 0},
            {"=", 0},
            {"]", 0},
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

        public static int GetBinaryPrecedence(string token)
        {
            return OperatorPrecedence.TryGetValue(token, out var precedence)
                ? precedence
                : 0;
        }

        static JsTokenUtils()
        {
            var n = new byte['e' + 1];
            n['0'] = n['1'] = n['2'] = n['3'] = n['4'] = n['5'] = n['6'] = n['7'] = n['8'] = n['9'] = n['.'] = True;
            ValidNumericChars = n;

            var o = new byte['|' + 1];
            o['<'] = o['>'] = o['='] = o['!'] = o['+'] = o['-'] = o['*'] = o['/'] = o['|'] = o['&'] = o['^'] = True;
            OperatorChars = o;

            var a = new byte['z' + 1];
            for (var i = (int) '0'; i < a.Length; i++)
            {
                if (i >= 'A' && i <= 'Z' || i >= 'a' && i <= 'z' || i >= '0' && i <= '9' || i == '_')
                    a[i] = True;
            }
            ValidVarNameChars = a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNumericChar(this char c) => c < ValidNumericChars.Length && ValidNumericChars[c] == True;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidVarNameChar(this char c) => c < ValidVarNameChars.Length && ValidVarNameChars[c] == True;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOperatorChar(this char c) => c < OperatorChars.Length && OperatorChars[c] == True;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBindingExpressionChar(this char c) => c == '.' || c == '(' || c == '[';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringSegment AdvancePastWhitespace(this StringSegment literal)
        {
            var i = 0;
            while (i < literal.Length && literal.GetChar(i).IsWhiteSpace())
                i++;

            return i == 0 ? literal : literal.Subsegment(i < literal.Length ? i : literal.Length);
        }

        public static bool EvaluateToBool(this JsToken token, TemplateScopeContext scope)
        {
            var ret = token.Evaluate(scope);
            if (ret is bool b)
                return b;
            
            throw new ArgumentException($"Expected bool expression but instead received '{token}'");
        }

        public static object UnwrapValue(this JsToken token)
        {
            if (token is JsLiteral c)
                return c.Value;
            if (token is JsLiteralExpression l)
                return l.Value;
            return token;
        }

        internal static string StripQuotes(this StringSegment arg) => arg.HasValue ? StripQuotes(arg.Value) : string.Empty;
        internal static string StripQuotes(this string arg)
        {
            if (arg == null || arg.Length < 2)
                return arg;

            switch (arg[0])
            {
                case '"':
                case '`':
                case '\'':
                case '′':
                    return arg.Substring(1, arg.Length - 2);
            }

            return arg;
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

                c = literal.GetChar(0);
                if (c == ')')
                {
                    literal = literal.Advance(1);
                    token = bracketsExpr;
                    return literal;
                }
                
                throw new ArgumentException($"Invalid syntax: Expected ')' but instead found '{c}': {literal.SubstringWithElipsis(0, 50)}");
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
                i = 1;
                var hasEscapeChar = false;
                while (i < literal.Length && ((c = literal.GetChar(i)) != firstChar || literal.GetChar(i - 1) == '\\'))
                {
                    i++;
                    if (!hasEscapeChar)
                        hasEscapeChar = c == '\\';
                }

                if (i >= literal.Length || literal.GetChar(i) != firstChar)
                    throw new ArgumentException($"Unterminated string literal: {literal}");

                var str = literal.Substring(1, i - 1);
                token = new JsLiteral(str);

                if (hasEscapeChar)
                {
                    var sb = StringBuilderCache.Allocate();
                    for (var j = 0; j < str.Length; j++)
                    {
                        // strip the back-slash used to escape quote char in strings
                        var ch = str[j];
                        if (ch != '\\' || (j + 1 >= str.Length || str[j + 1] != firstChar))
                            sb.Append(ch);
                    }
                    token = new JsLiteral(StringBuilderCache.ReturnAndFree(sb));
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

                    literal = literal.ParseJsToken(out var mapKeyToken);

                    if (!(mapKeyToken is JsLiteral) && !(mapKeyToken is JsIdentifier)) 
                        throw new NotSupportedException($"'{mapKeyToken}' is not a valid Object key, expected literal or identifier.");

                    JsToken mapValueToken;
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
                            throw new ArgumentException($"Unterminated object literal near: {literal.SubstringWithElipsis(0, 50)}");
                            
                        mapValueToken = mapKeyToken;
                    }
                    
                    props.Add(new JsProperty(mapKeyToken, mapValueToken, shorthand));

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
                var elements = new List<JsToken>();

                literal = literal.Advance(1);
                while (!literal.IsNullOrEmpty())
                {
                    literal = literal.AdvancePastWhitespace();
                    if (literal.GetChar(0) == ']')
                    {
                        literal = literal.Advance(1);
                        break;
                    }

                    literal = literal.ParseJsExpression(out var listValue);
                    elements.Add(listValue);

                    literal = literal.AdvancePastWhitespace();
                    if (literal.IsNullOrEmpty())
                        break;
                    
                    if (literal.GetChar(0) == ']')
                    {
                        literal = literal.Advance(1);
                        break;
                    }

                    literal = literal.AdvancePastWhitespace();
                    c = literal.GetChar(0);
                    if (c == ']')
                    {
                        literal = literal.Advance(1);
                        break;
                    }
                    
                    if (c != ',')
                        throw new ArgumentException($"Unterminated array literal near: {literal.SubstringWithElipsis(0, 50)}");
                    
                    literal = literal.Advance(1);
                    literal = literal.AdvancePastWhitespace();
                }

                literal = literal.AdvancePastWhitespace();

                token = new JsArrayExpression(elements);
                return literal;
            }
            if (literal.StartsWith("true") && (literal.Length == 4 || !IsValidVarNameChar(literal.GetChar(4))))
            {
                token = JsLiteral.True;
                return literal.Advance(4);
            }
            if (literal.StartsWith("false") && (literal.Length == 5 || !IsValidVarNameChar(literal.GetChar(5))))
            {
                token = JsLiteral.False;
                return literal.Advance(5);
            }
            if (literal.StartsWith("null") && (literal.Length == 4 || !IsValidVarNameChar(literal.GetChar(4))))
            {
                token = JsNull.Value;
                return literal.Advance(4);
            }
            if (literal.StartsWith("and") && (literal.Length == 3 || !IsValidVarNameChar(literal.GetChar(3))))
            {
                token = JsAnd.Operator;
                return literal.Advance(3);
            }
            if (literal.StartsWith("or") && (literal.Length == 2 || !IsValidVarNameChar(literal.GetChar(2))))
            {
                token = JsOr.Operator;
                return literal.Advance(2);
            }
            if (firstChar.IsOperatorChar())
            {
                if (literal.StartsWith(">="))
                {
                    token = JsGreaterThanEqual.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("<="))
                {
                    token = JsLessThanEqual.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("!=="))
                {
                    token = JsStrictNotEquals.Operator;
                    return literal.Advance(3);
                }
                if (literal.StartsWith("!="))
                {
                    token = JsNotEquals.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("==="))
                {
                    token = JsStrictEquals.Operator;
                    return literal.Advance(3);
                }
                if (literal.StartsWith("=="))
                {
                    token = JsEquals.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("||"))
                {
                    token = JsOr.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("&&"))
                {
                    token = JsAnd.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith("<<"))
                {
                    token = JsBitwiseLeftShift.Operator;
                    return literal.Advance(2);
                }
                if (literal.StartsWith(">>"))
                {
                    token = JsBitwiseRightShift.Operator;
                    return literal.Advance(2);
                }

                switch (firstChar)
                {
                    case '>':
                        token = JsGreaterThan.Operator;
                        return literal.Advance(1);
                    case '<':
                        token = JsLessThan.Operator;
                        return literal.Advance(1);
                    case '=':
                        token = JsAssignment.Operator;
                        return literal.Advance(1);
                    case '!':
                        token = JsNot.Operator;
                        return literal.Advance(1);
                    case '+':
                        token = JsAddition.Operator;
                        return literal.Advance(1);
                    case '-':
                        token = JsSubtraction.Operator;
                        return literal.Advance(1);
                    case '*':
                        token = JsMultiplication.Operator;
                        return literal.Advance(1);
                    case '/':
                        token = JsDivision.Operator;
                        return literal.Advance(1);
                    case '&':
                        token = JsBitwiseAnd.Operator;
                        return literal.Advance(1);
                    case '|':
                        token = JsBitwiseOr.Operator;
                        return literal.Advance(1);
                    case '^':
                        token = JsBitwiseXOr.Operator;
                        return literal.Advance(1);
                    case '%':
                        token = JsMod.Operator;
                        return literal.Advance(1);
                    default:
                        throw new NotSupportedException($"Invalid Operator found near: '{literal.SubstringWithElipsis(0, 50)}'");
                }
            }

            // name
            i = 1;
            var isExpression = false;
            var hadWhitespace = false;
            while (i < literal.Length && IsValidVarNameChar(c = literal.GetChar(i)) || 
                   (isExpression = c.IsBindingExpressionChar() || (filterExpression && c == ':')))
            {
                if (isExpression)
                {
                    literal = literal.ParseCallExpression(out JsCallExpression expr);
                    token = expr;
                    return literal;
                }
                
                i++;

                while (i < literal.Length && literal.GetChar(i).IsWhiteSpace()) // advance past whitespace
                {
                    i++;
                    hadWhitespace = true;
                }
                
                if (hadWhitespace && (i >= literal.Length || !literal.GetChar(i).IsBindingExpressionChar()))
                    break;
            }

            token = new JsIdentifier(literal.Subsegment(0, i).TrimEnd());
            return literal.Advance(i);
        }
    }

    public class JsCallExpression : JsToken
    {
        public JsCallExpression()
        {
            Args = new List<StringSegment>();
        }
        
        public JsCallExpression(string name) : this() => Name = name.ToStringSegment();
        public JsCallExpression(StringSegment name) : this() => Name = name;

        public StringSegment Name { get; set; }

        private string nameString;
        public string NameString => nameString ?? (nameString = Name.HasValue ? Name.Value : null);

        public List<StringSegment> Args { get; internal set; }

        public StringSegment Original { get; set; }

        private string originalString;
        public string OriginalString => originalString ?? (originalString = Original.HasValue ? Original.Value : null);

        private bool? isBinding = null;
        public bool IsBinding => (bool)(isBinding ?? (isBinding = DetectBinding(this)));

        public virtual int IndexOfMethodEnd(StringSegment commandString, int pos) => pos;

        private static bool DetectBinding(JsCallExpression cmd)
        {
            var i = 0;
            char c;
            var isBinding = false;
            while (i < cmd.Name.Length && 
                   ((c = cmd.Name.GetChar(i)).IsValidVarNameChar() || (isBinding = (c == '.' || c == '[' || c.IsWhiteSpace()))))
            {
                if (isBinding)
                    return true;
                i++;
            }
            return false;
        }

        public override object Evaluate(TemplateScopeContext scope)
        {
            var value = IsBinding
                ? scope.PageResult.EvaluateBinding(NameString, scope)
                : Args.Count > 0 
                    ? scope.PageResult.EvaluateMethod(this, scope) 
                    : scope.PageResult.EvaluateBindingExpression(Name, scope);
            return value;
        }

        //Output different format for debugging to verify command was parsed correctly
        public virtual string ToDebugString()
        {
            var sb = StringBuilderCacheAlt.Allocate();
            foreach (var arg in Args)
            {
                if (sb.Length > 0)
                    sb.Append('|');
                sb.Append(arg);
            }
            return $"[{Name}:{StringBuilderCacheAlt.ReturnAndFree(sb)}]";
        }

        public override string ToString()
        {
            var sb = StringBuilderCacheAlt.Allocate();
            foreach (var arg in Args)
            {
                if (sb.Length > 0)
                    sb.Append(',');
                sb.Append(JsonValue(arg));
            }
            return $"{Name}({StringBuilderCacheAlt.ReturnAndFree(sb)})";
        }

        public StringSegment ToStringSegment() => ToString().ToStringSegment();
        
        public override string ToRawString() => (":" + ToString()).EncodeJson();

        public string GetDisplayName() => (NameString ?? "").Replace('′', '"');

        protected bool Equals(JsCallExpression other)
        {
            return string.Equals(nameString, other.nameString) && 
                   string.Equals(originalString, other.originalString) && 
                   isBinding == other.isBinding;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsCallExpression) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (nameString != null ? nameString.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (originalString != null ? originalString.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ isBinding.GetHashCode();
                return hashCode;
            }
        }
    }

    public static class CallExpressionUtils
    {
        public static List<JsCallExpression> ParseCallExpression(this StringSegment commandsString, char separator = ',', Func<StringSegment, int, int?> atEndIndex = null) 
            => commandsString.ParseFilterExpression<JsCallExpression>(out int _, separator, atEndIndex);

        public static List<JsCallExpression> ParseCallExpression(this StringSegment commandsString, out int pos, char separator = ',', Func<StringSegment, int, int?> atEndIndex = null) 
            => commandsString.ParseFilterExpression<JsCallExpression>(out pos, separator, atEndIndex);

        public static List<T> ParseFilterExpression<T>(this StringSegment commandsString, char separator = ',', Func<StringSegment, int, int?> atEndIndex = null, bool allowWhitespaceSensitiveSyntax = false) 
            where T : JsCallExpression, new()
            => commandsString.ParseFilterExpression<T>(out int _, separator, atEndIndex, allowWhitespaceSensitiveSyntax);

        public static List<T> ParseFilterExpression<T>(this StringSegment commandsString, out int pos, char separator = ',',
            Func<StringSegment, int, int?> atEndIndex = null, bool allowWhitespaceSensitiveSyntax = false)
            where T : JsCallExpression, new()
        {
            var to = new List<T>();
            List<StringSegment> args = null;
            pos = 0;

            if (commandsString.IsNullOrEmpty())
                return to;

            var inDoubleQuotes = false;
            var inSingleQuotes = false;
            var inBackTickQuotes = false;
            var inPrimeQuotes = false;
            var inBrackets = false;

            var endBlockPos = commandsString.Length;
            var cmd = new T();

            try
            {
                for (var i = 0; i < commandsString.Length; i++)
                {
                    var c = commandsString.GetChar(i);
                    if (c.IsWhiteSpace())
                        continue;

                    if (inDoubleQuotes)
                    {
                        if (c == '"')
                            inDoubleQuotes = false;
                        continue;
                    }
                    if (inSingleQuotes)
                    {
                        if (c == '\'')
                            inSingleQuotes = false;
                        continue;
                    }
                    if (inBackTickQuotes)
                    {
                        if (c == '`')
                            inBackTickQuotes = false;
                        continue;
                    }
                    if (inPrimeQuotes)
                    {
                        if (c == '′')
                            inPrimeQuotes = false;
                        continue;
                    }
                    switch (c)
                    {
                        case '"':
                            inDoubleQuotes = true;
                            continue;
                        case '\'':
                            inSingleQuotes = true;
                            continue;
                        case '`':
                            inBackTickQuotes = true;
                            continue;
                        case '′':
                            inPrimeQuotes = true;
                            continue;
                    }

                    if (c.IsOperatorChar() && // don't take precedence over '|' seperator 
                        (c != separator || (i + 1 < commandsString.Length && commandsString.GetChar(i + 1).IsOperatorChar())))
                    {
                        cmd.Name = commandsString.Subsegment(0, i).TrimEnd();
                        pos = i;
                        if (cmd.Name.HasValue)
                            to.Add(cmd);
                        return to;
                    }

                    if (allowWhitespaceSensitiveSyntax && c == ':')
                    {
                        // replace everything after ':' up till new line and rewrite as single string to method
                        var endStringPos = commandsString.IndexOf("\n", i);
                        var endStatementPos = commandsString.IndexOf("}}", i);

                        if (endStringPos == -1 || (endStatementPos != -1 && endStatementPos < endStringPos))
                            endStringPos = endStatementPos;
                        
                        if (endStringPos == -1)
                            throw new NotSupportedException($"Whitespace sensitive syntax did not find a '\\n' new line to mark the end of the statement, near '{commandsString.SubstringWithElipsis(i,50)}'");

                        cmd.Name = commandsString.Subsegment(pos, i - pos).Trim();
                        
                        var originalArgs = commandsString.Substring(i + 1, endStringPos - i - 1);
                        var rewrittenArgs = "′" + originalArgs.Trim().Replace("{", "{{").Replace("}", "}}").Replace("′", "\\′") + "′)";
                        ParseArguments(rewrittenArgs.ToStringSegment(), out args);
                        cmd.Args = args;
                        
                        i = endStringPos == endStatementPos 
                            ? endStatementPos - 2  //move cursor back before var block terminator 
                            : endStringPos;

                        pos = i + 1;
                        continue;
                    }

                    if (c == '(')
                    {
                        inBrackets = true;
                        cmd.Name = commandsString.Subsegment(pos, i - pos).Trim();
                        pos = i + 1;

                        var literal = commandsString.Subsegment(pos);
                        var literalRemaining = ParseArguments(literal, out args);
                        cmd.Args = args;
                        var endPos = literal.Length - literalRemaining.Length;
                        
                        i += endPos;
                        pos = i + 1;
                        continue;
                    }
                    if (c == ')')
                    {
                        inBrackets = false;
                        pos = i + 1;

                        pos = cmd.IndexOfMethodEnd(commandsString, pos);
                        continue;
                    }

                    if (inBrackets && c == ',')
                    {
                        var arg = commandsString.Subsegment(pos, i - pos).Trim();
                        cmd.Args.Add(arg);
                        pos = i + 1;
                    }
                    else if (c == separator)
                    {
                        if (!cmd.Name.HasValue)
                            cmd.Name = commandsString.Subsegment(pos, i - pos).Trim();

                        to.Add(cmd);
                        cmd = new T();
                        pos = i + 1;
                    }
                    
                    var atEndIndexPos = atEndIndex?.Invoke(commandsString, i);
                    if (atEndIndexPos != null)
                    {
                        endBlockPos = atEndIndexPos.Value;
                        break;
                    }
                }

                var remaining = commandsString.Subsegment(pos, endBlockPos - pos);
                if (!remaining.Trim().IsNullOrEmpty())
                {
                    pos += remaining.Length;
                    cmd.Name = remaining.Trim();
                }

                if (!cmd.Name.IsNullOrEmpty())
                    to.Add(cmd);
            }
            catch (Exception e)
            {
                throw new Exception($"Illegal syntax near '{commandsString.Value.SafeSubstring(pos - 10, 50)}...'", e);
            }

            return to;
        }

        // ( {args} , {args} )
        //   ^
        public static StringSegment ParseArguments(StringSegment argsString, out List<StringSegment> args)
        {
            var to = new List<StringSegment>();

            var inDoubleQuotes = false;
            var inSingleQuotes = false;
            var inBackTickQuotes = false;
            var inPrimeQuotes = false;
            var inBrackets = 0;
            var inParens = 0;
            var inBraces = 0;
            var lastPos = 0;

            for (var i = 0; i < argsString.Length; i++)
            {
                var c = argsString.GetChar(i);
                if (inDoubleQuotes)
                {
                    if (c == '"')
                        inDoubleQuotes = false;
                    continue;
                }
                if (inSingleQuotes)
                {
                    if (c == '\'')
                        inSingleQuotes = false;
                    continue;
                }
                if (inBackTickQuotes)
                {
                    if (c == '`')
                        inBackTickQuotes = false;
                    continue;
                }
                if (inPrimeQuotes)
                {
                    if (c == '′')
                        inPrimeQuotes = false;
                    continue;
                }
                if (inBrackets > 0)
                {
                    if (c == '[')
                        ++inBrackets;
                    else if (c == ']')
                        --inBrackets;
                    continue;
                }
                if (inBraces > 0)
                {
                    if (c == '{')
                        ++inBraces;
                    else if (c == '}')
                        --inBraces;
                    continue;
                }
                if (inParens > 0)
                {
                    if (c == '(')
                        ++inParens;
                    else if (c == ')')
                        --inParens;
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inDoubleQuotes = true;
                        continue;
                    case '\'':
                        inSingleQuotes = true;
                        continue;
                    case '`':
                        inBackTickQuotes = true;
                        continue;
                    case '′':
                        inPrimeQuotes = true;
                        continue;
                    case '[':
                        inBrackets++;
                        continue;
                    case '{':
                        inBraces++;
                        continue;
                    case '(':
                        inParens++;
                        continue;
                    case ',':
                    {
                        var arg = argsString.Subsegment(lastPos, i - lastPos).Trim();
                        to.Add(arg);
                        lastPos = i + 1;
                        continue;
                    }
                    case ')':
                    {
                        var arg = argsString.Subsegment(lastPos, i - lastPos).Trim();
                        if (!arg.IsNullOrEmpty())
                        {
                            to.Add(arg);
                        }

                        args = to;
                        return argsString.Advance(i);
                    }
                }
            }
            
            args = to;
            return TypeConstants.EmptyStringSegment;
        }
        
        public static StringSegment ParseCallExpression(this StringSegment literal, out JsCallExpression binding)
        {
            var inDoubleQuotes = false;
            var inSingleQuotes = false;
            var inBackTickQuotes = false;
            var inPrimeQuotes = false;
            var inBrackets = 0;
            var inBraces = 0;
            var lastPos = 0;

            for (var i = 0; i < literal.Length; i++)
            {
                var c = literal.GetChar(i);
                if (c.IsWhiteSpace())
                    continue;
                
                if (inDoubleQuotes)
                {
                    if (c == '"')
                        inDoubleQuotes = false;
                    continue;
                }
                if (inSingleQuotes)
                {
                    if (c == '\'')
                        inSingleQuotes = false;
                    continue;
                }
                if (inBackTickQuotes)
                {
                    if (c == '`')
                        inBackTickQuotes = false;
                    continue;
                }
                if (inPrimeQuotes)
                {
                    if (c == '′')
                        inPrimeQuotes = false;
                    continue;
                }
                if (inBrackets > 0)
                {
                    if (c == '[')
                        ++inBrackets;
                    if (c == ']')
                        --inBrackets;
                    continue;
                }
                if (inBraces > 0)
                {
                    if (c == '{')
                        ++inBraces;
                    if (c == '}')
                        --inBraces;
                    continue;
                }
                
                if (c == ':') //whitespace sensitive syntax
                {
                    // replace everything after ':' up till new line and rewrite as single string to method
                    var endStringPos = literal.IndexOf("\n", i);
                    var endStatementPos = literal.IndexOf("}}", i);

                    if (endStringPos == -1 || (endStatementPos != -1 && endStatementPos < endStringPos))
                        endStringPos = endStatementPos;
                        
                    if (endStringPos == -1)
                        throw new NotSupportedException($"Whitespace sensitive syntax did not find a '\\n' new line to mark the end of the statement, near '{literal.SubstringWithElipsis(i,50)}'");

                    binding = new JsCallExpression(literal.Subsegment(0, i).Trim());

                    var originalArgs = literal.Substring(i + 1, endStringPos - i - 1);
                    var rewrittenArgs = "′" + originalArgs.Trim().Replace("{","{{").Replace("}","}}").Replace("′", "\\′") + "′)";
                    ParseArguments(rewrittenArgs.ToStringSegment(), out List<StringSegment> args);
                    binding.Args = args;
                    return literal.Subsegment(endStringPos);
                }
                
                if (c == '(')
                {
                    var pos = i + 1;
                    binding = new JsCallExpression(literal.Subsegment(0, i).Trim());
                    literal = ParseArguments(literal.Subsegment(pos), out List<StringSegment> args);
                    binding.Args = args;
                    return literal.Advance(1);
                }

                switch (c)
                {
                    case '"':
                        inDoubleQuotes = true;
                        continue;
                    case '\'':
                        inSingleQuotes = true;
                        continue;
                    case '`':
                        inBackTickQuotes = true;
                        continue;
                    case '′':
                        inPrimeQuotes = true;
                        continue;
                    case '[':
                        inBrackets++;
                        continue;
                    case '{':
                        inBraces++;
                        continue;
                }

                if (!(c.IsValidVarNameChar() || c.IsBindingExpressionChar()))
                {
                    binding = new JsCallExpression(literal.Subsegment(lastPos, i - lastPos).Trim());
                    return literal.Advance(i);
                }
            }

            binding = new JsCallExpression(literal.Subsegment(0, literal.Length));
            return TypeConstants.EmptyStringSegment;
        }
        
    }
}