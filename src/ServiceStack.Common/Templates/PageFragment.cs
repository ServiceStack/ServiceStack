using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ServiceStack.Text;
#if NETSTANDARD1_3
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public abstract class PageFragment {}

    public class PageVariableFragment : PageFragment
    {
        public StringSegment OriginalText { get; set; }
        private byte[] originalTextBytes;
        public byte[] OriginalTextBytes => originalTextBytes ?? (originalTextBytes = OriginalText.ToUtf8Bytes());
        
        public StringSegment Name { get; set; }
        private string nameString;
        public string NameString => nameString ?? (nameString = Name.Value);
        
        public object Value { get; set; }
        
        public Command Command { get; set; }
        
        public Command[] FilterCommands { get; set; }

        public PageVariableFragment(StringSegment originalText, StringSegment name, List<Command> filterCommands)
        {
            OriginalText = originalText;
            FilterCommands = filterCommands?.ToArray() ?? TypeConstants<Command>.EmptyArray;

            ParseLiteral(name, out StringSegment outName, out object value, out Command command);

            Name = outName;
            Value = value;
            Command = command;
        }

        public void ParseLiteral(StringSegment literal, out StringSegment name, out object value, out Command cmd)
        {
            try
            {
                literal.ParseNextToken(out name, out value, out cmd);
            }
            catch (ArgumentException e)
            {
                throw new Exception($"Invalid literal: {literal} in '{OriginalText}'", e);
            }
        }
    }

    public class PageStringFragment : PageFragment
    {
        public StringSegment Value { get; set; }

        private byte[] valueBytes;
        public byte[] ValueBytes => valueBytes ?? (valueBytes = Value.ToUtf8Bytes());

        public PageStringFragment(StringSegment value)
        {
            Value = value;
        }
    }

    public static class PageFragmentUtils
    {
        private static readonly byte[] ValidNumericChars;
        private static readonly byte[] ValidVarNameChars;
        private const byte True = 1;

        static PageFragmentUtils()
        {
            var n = new byte['e' + 1];
            n['0'] = n['1'] = n['2'] = n['3'] = n['4'] = n['5'] = n['6'] = n['7'] = n['8'] = n['9'] = n['.'] = True;
            ValidNumericChars = n;
            
            var a = new byte['z' + 1];
            for (var i = (int)'A'; i < a.Length; i++)
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
        
        public static bool IsBinding(this Command cmd)
        {
            var i = 0;
            char c;
            var isBinding = false;
            while (i < cmd.Name.Length && (IsValidVarNameChar(c = cmd.Name.GetChar(i)) || (isBinding = (c == '.' || c == '[' || char.IsWhiteSpace(c)))))
            {
                if (isBinding)
                    return true;
                i++;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWhiteSpace(char c) => c == ' ' || (c >= '\x0009' && c <= '\x000d') || c == '\x00a0' || c == '\x0085';

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
            var c =(char)0;
            while (i < literal.Length && (c = literal.GetChar(i)) != delim)
                i++;

            if (c == delim)
                return literal.Subsegment(i + 1);
                
            return i == 0 ? literal : literal.Subsegment(i < literal.Length ? i : literal.Length);
        }

        public static StringSegment AdvancePastAnyChar(this StringSegment literal, char delim1, char delim2)
        {
            var i = 0;
            var c =(char)0;
            while (i < literal.Length && (c = literal.GetChar(i)) != delim1 && c != delim2)
                i++;

            if (c == delim1 || c == delim2)
                return literal.Subsegment(i + 1);
                
            return i == 0 ? literal : literal.Subsegment(i < literal.Length ? i : literal.Length);
        }

        public static StringSegment ParseNextToken(this StringSegment literal, out StringSegment name, out object value, out Command cmd)
        {
            name = default(StringSegment);
            value = null;
            cmd = null;
            var c = (char)0;

            if (literal.IsNullOrEmpty())
                return TypeConstants.EmptyStringSegment;

            var i = 0;
            literal = literal.AdvancePastWhitespace();
            
            var firstChar = literal.GetChar(0);
            if (firstChar == '\'' || firstChar == '"')
            {
                i = 1;
                while (literal.GetChar(i) != firstChar || literal.GetChar(i-1) == '\\')
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
                
                while (i < literal.Length && IsValidNumericChar(c = literal.GetChar(i)) || (hasExponent = (c == 'e' || c == 'E')))
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

                literal = literal.Subsegment(1);
                
                while (!literal.IsNullOrEmpty() && literal.GetChar(0) != '}')
                {
                    literal = literal.ParseNextToken(out StringSegment mapKeyVar, out object mapKeyString, out _);
                    var mapKey = mapKeyVar.HasValue
                        ? mapKeyVar.Value
                        : (string) mapKeyString;
                    
                    if (mapKey != null)
                    {
                        literal = literal.AdvancePastChar(':');
                        literal = literal.ParseNextToken(out StringSegment mapVarRef, out object mapValue, out _);
                        
                        if (mapVarRef.HasValue)
                            map[mapKey] = new VarRef(mapVarRef.Value);
                        else
                            map[mapKey] = mapValue;
                    }

                    literal = literal.AdvancePastAnyChar(',', '}');
                    literal = literal.AdvancePastWhitespace();
                }

                value = map;
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
                value = NullValue.Instance;
                return literal.Advance(4);
            }
            
            // name
            i = 1;
            var isExpression = false;
            while (i < literal.Length && IsValidVarNameChar(c = literal.GetChar(i)) || (isExpression = (c == '.' || c == '(' || c == '[')))
            {
                if (isExpression)
                {
                    cmd = literal.ParseCommands(out int pos).FirstOrDefault();
                    return literal.Advance(pos);
                }
                    
                i++;
            }
                    
            name = literal.Subsegment(0, i);
            return literal.Advance(i);
        }
    }
}