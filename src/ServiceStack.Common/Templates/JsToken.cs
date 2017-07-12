using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using ServiceStack.Text;

#if NETSTANDARD1_3
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates //TODO move to ServiceStack.Text when baked
{
    public abstract class JsToken : IRawString
    {
        public abstract string ToRawString();

        public string JsonValue(object value)
        {
            if (value == null || value == JsNull.Instance)
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
            js.ParseNextToken(out object value, out JsBinding binding);
            
            if (binding != null)
                return binding;

            if (value is string s)
                return new JsString(s);
            if (value is int i)
                return new JsNumber(i);
            if (value is long l)
                return new JsNumber(l);
            if (value is double d)
                return new JsNumber(d);
            if (value is List<object> list)
                return new JsArray(list);
            if (value is Dictionary<string,object> map)
                return new JsObject(map);
            if (value is null || value == JsNull.Instance)
                return JsNull.Instance;

            throw new NotSupportedException($"Unknown value JsToken '{value}'");
        }
    }

    public class JsNull : JsToken
    {
        private JsNull() {} //this is the only one
        public static JsNull Instance = new JsNull();
        public override string ToRawString() => "null";
    }

    public class JsBinding : JsToken
    {
        public virtual StringSegment Binding { get; }

        public JsBinding(){}
        public JsBinding(string binding) => Binding = binding.ToStringSegment();
        public JsBinding(StringSegment binding) => Binding = binding;
        public override string ToRawString() => ":" + Binding;

        protected bool Equals(JsBinding other) => string.Equals(Binding, other.Binding);
        public override int GetHashCode() => Binding.GetHashCode();

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsBinding) obj);
        }
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
    }

    public static class JsTokenUtils
    {
        private static readonly byte[] ValidNumericChars;
        private static readonly byte[] ValidVarNameChars;
        private const byte True = 1;

        static JsTokenUtils()
        {
            var n = new byte['e' + 1];
            n['0'] = n['1'] = n['2'] = n['3'] = n['4'] = n['5'] = n['6'] = n['7'] = n['8'] = n['9'] = n['.'] = True;
            ValidNumericChars = n;

            var a = new byte['z' + 1];
            for (var i = (int) 'A'; i < a.Length; i++)
            {
                if (i >= 'A' && i <= 'Z' || (i >= 'a' && i <= 'z') || i == '_')
                    a[i] = True;
            }
            ValidVarNameChars = a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidNumericChar(char c) => c < ValidNumericChars.Length && ValidNumericChars[c] == True;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValidVarNameChar(char c) => c < ValidVarNameChars.Length && ValidVarNameChars[c] == True;

        public static bool IsBinding(this JsExpression cmd)
        {
            var i = 0;
            char c;
            var isBinding = false;
            while (i < cmd.Name.Length && 
                   (IsValidVarNameChar(c = cmd.Name.GetChar(i)) || (isBinding = (c == '.' || c == '[' || IsWhiteSpace(c)))))
            {
                if (isBinding)
                    return true;
                i++;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWhiteSpace(char c) =>
            c == ' ' || (c >= '\x0009' && c <= '\x000d') || c == '\x00a0' || c == '\x0085';

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static StringSegment AdvancePastWhitespace(this StringSegment literal)
        {
            var i = 0;
            while (i < literal.Length && IsWhiteSpace(literal.GetChar(i)))
                i++;

            return i == 0 ? literal : literal.Subsegment(i < literal.Length ? i : literal.Length);
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

        public static StringSegment AdvancePastAnyChar(this StringSegment literal, char delim1, char delim2)
        {
            var i = 0;
            var c = (char) 0;
            while (i < literal.Length && (c = literal.GetChar(i)) != delim1 && c != delim2)
                i++;

            if (c == delim1 || c == delim2)
                return literal.Subsegment(i + 1);

            return i == 0 ? literal : literal.Subsegment(i < literal.Length ? i : literal.Length);
        }

        public static StringSegment ParseNextToken(this StringSegment literal, out object value, out JsBinding binding)
        {
            binding = null;
            value = null;
            var c = (char) 0;

            if (literal.IsNullOrEmpty())
                return TypeConstants.EmptyStringSegment;

            var i = 0;
            literal = literal.AdvancePastWhitespace();

            var firstChar = literal.GetChar(0);
            if (firstChar == '\'' || firstChar == '"')
            {
                i = 1;
                while (literal.GetChar(i) != firstChar || literal.GetChar(i - 1) == '\\')
                    i++;

                if (i >= literal.Length || literal.GetChar(i) != firstChar)
                    throw new ArgumentException($"Unterminated string literal: {literal}");

                value = literal.Substring(1, i - 1);
                return literal.Advance(i);
            }
            if (firstChar >= '0' && firstChar <= '9' || firstChar == '-' || firstChar == '+')
            {
                i = 1;
                var hasExponent = false;
                var hasDecimal = false;

                while (i < literal.Length && IsValidNumericChar(c = literal.GetChar(i)) ||
                       (hasExponent = (c == 'e' || c == 'E')))
                {
                    if (c == '.')
                        hasDecimal = true;

                    i++;

                    if (hasExponent)
                    {
                        i += 2; // [e+1]0

                        while (i < literal.Length && IsValidNumericChar(literal.GetChar(i)))
                            i++;

                        break;
                    }
                }

                var numLiteral = literal.Subsegment(0, i);

                //don't convert into ternary to avoid Type coercion
                if (hasDecimal || hasExponent)
                    value = numLiteral.TryParseDouble(out double d) ? d : default(double);
                else
                    value = numLiteral.ParseSignedInteger();

                return literal.Advance(i);
            }
            if (firstChar == '{')
            {
                var map = new Dictionary<string, object>();

                literal = literal.Advance(1);
                while (!literal.IsNullOrEmpty())
                {
                    literal = literal.ParseNextToken(out object mapKeyString, out JsBinding mapKeyVar);
                        
                    if (mapKeyVar is JsExpression)
                        throw new NotSupportedException($"JsExpression '{mapKeyVar?.Binding}' is not a valid Object key.");
                    
                    var mapKey = mapKeyVar != null
                        ? mapKeyVar.Binding.Value
                        : (string) mapKeyString;

                    if (mapKey != null)
                    {
                        literal = literal.AdvancePastChar(':');
                        literal = literal.ParseNextToken(out object mapValue, out JsBinding mapValueBinding);
                        map[mapKey] = mapValue ?? mapValueBinding;
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

                value = map;
                return literal;
            }
            if (firstChar == '[')
            {
                var list = new List<object>();

                literal = literal.Advance(1);
                while (!literal.IsNullOrEmpty() && literal.GetChar(0) != ']')
                {
                    literal = literal.ParseNextToken(out object mapValue, out JsBinding mapVarRef);
                    list.Add(mapVarRef ?? mapValue);

                    literal = literal.AdvancePastWhitespace();
                    if (literal.IsNullOrEmpty())
                        break;
                    
                    if (literal.GetChar(0) == ']')
                    {
                        literal = literal.Advance(1);
                        break;
                    }

                    literal = literal.AdvancePastChar(',');
                    literal = literal.AdvancePastWhitespace();
                }

                value = list;
                return literal;
            }
            if (literal.StartsWith("true") && (literal.Length == 4 || !IsValidVarNameChar(literal.GetChar(4))))
            {
                value = true;
                return literal.Advance(4);
            }
            if (literal.StartsWith("false") && (literal.Length == 5 || !IsValidVarNameChar(literal.GetChar(5))))
            {
                value = false;
                return literal.Advance(5);
            }
            if (literal.StartsWith("null") && (literal.Length == 4 || !IsValidVarNameChar(literal.GetChar(4))))
            {
                value = JsNull.Instance;
                return literal.Advance(4);
            }

            // name
            i = 1;
            var isExpression = false;
            while (i < literal.Length && IsValidVarNameChar(c = literal.GetChar(i)) ||
                   (isExpression = (c == '.' || c == '(' || c == '[')))
            {
                if (isExpression)
                {
                    binding = literal.ParseJsExpression(out int pos).FirstOrDefault();
                    return literal.Advance(pos);
                }

                i++;
            }

            binding = new JsBinding(literal.Subsegment(0, i));
            return literal.Advance(i);
        }
    }

    public class JsExpression : JsBinding
    {
        public JsExpression()
        {
            Args = new List<StringSegment>();
        }

        public StringSegment Name { get; set; }

        public List<StringSegment> Args { get; set; }

        public StringSegment Original { get; set; }

        public virtual int IndexOfMethodEnd(StringSegment commandString, int pos) => pos;
        
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

        public override StringSegment Binding => Original;
    }

    public static class JsExpressionUtils
    {
        public static List<JsExpression> ParseJsExpression(this StringSegment commandsString, char separator = ',', Func<StringSegment, int, int?> atEndIndex = null) 
            => commandsString.ParseExpression<JsExpression>(out int _, separator, atEndIndex);

        public static List<JsExpression> ParseJsExpression(this StringSegment commandsString, out int pos, char separator = ',', Func<StringSegment, int, int?> atEndIndex = null) 
            => commandsString.ParseExpression<JsExpression>(out pos, separator, atEndIndex);

        public static List<T> ParseExpression<T>(this StringSegment commandsString, char separator = ',', Func<StringSegment, int, int?> atEndIndex = null) 
            where T : JsExpression, new()
            => commandsString.ParseExpression<T>(out int _, separator, atEndIndex);

        public static List<T> ParseExpression<T>(this StringSegment commandsString, out int pos, char separator = ',',
            Func<StringSegment, int, int?> atEndIndex = null)
            where T : JsExpression, new()
        {
            var to = new List<T>();
            pos = 0;

            if (commandsString.IsNullOrEmpty())
                return to;

            var inDoubleQuotes = false;
            var inSingleQuotes = false;
            var inBrackets = false;

            var endBlockPos = commandsString.Length;
            var cmd = new T();

            try
            {
                for (var i = 0; i < commandsString.Length; i++)
                {
                    var c = commandsString.GetChar(i);
                    if (char.IsWhiteSpace(c))
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
                    if (c == '"')
                    {
                        inDoubleQuotes = true;
                        continue;
                    }
                    if (c == '\'')
                    {
                        inSingleQuotes = true;
                        continue;
                    }

                    if (c == '(')
                    {
                        inBrackets = true;
                        cmd.Name = commandsString.Subsegment(pos, i - pos).Trim();
                        pos = i + 1;

                        cmd.Args = ParseArguments(commandsString.Subsegment(pos), out int endPos);
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
                    cmd.Name = remaining.Trim();

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
        public static List<StringSegment> ParseArguments(StringSegment argsString, out int endPos)
        {
            var to = new List<StringSegment>();

            var inDoubleQuotes = false;
            var inSingleQuotes = false;
            var inBrackets = 0;
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
                if (inBraces > 0)
                {
                    if (c == '{')
                        ++inBraces;
                    if (c == '}')
                        --inBraces;
                    continue;
                }
                if (inBrackets > 0)
                {
                    if (c == '(')
                        ++inBrackets;
                    if (c == ')')
                        --inBrackets;
                    continue;
                }

                if (c == '"')
                {
                    inDoubleQuotes = true;
                    continue;
                }
                if (c == '\'')
                {
                    inSingleQuotes = true;
                    continue;
                }
                if (c == '{')
                {
                    inBraces++;
                    continue;
                }
                if (c == '(')
                {
                    inBrackets++;
                    continue;
                }

                if (c == ',')
                {
                    var arg = argsString.Subsegment(lastPos, i - lastPos).Trim();
                    to.Add(arg);
                    lastPos = i + 1;
                    continue;
                }

                if (c == ')')
                {
                    var arg = argsString.Subsegment(lastPos, i - lastPos).Trim();
                    if (!arg.IsNullOrEmpty())
                    {
                        to.Add(arg);
                    }

                    endPos = i;
                    return to;
                }
            }

            endPos = argsString.Length;

            return to;
        }
    }
}