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
        
        public static void ParseNextToken(this StringSegment literal, out StringSegment name, out object value, out Command cmd)
        {
            name = default(StringSegment);
            value = null;
            cmd = null;

            if (literal.IsNullOrEmpty())
                return;

            var firstChar = literal.GetChar(0);
            if (firstChar == '\'' || firstChar == '"')
            {
                var i = 1;
                while (literal.GetChar(i) != firstChar || literal.GetChar(i-1) == '\\')
                    i++;
                
                if (i >= literal.Length || literal.GetChar(i) != firstChar)
                    throw new ArgumentException($"Unterminated string literal: {literal}");

                value = literal.Substring(1, i - 1);
            }
            else if (firstChar >= '0' && firstChar <= '9' || firstChar == '-' || firstChar == '+')
            {
                var i = 1;
                var c = (char)0;
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

                var numLiteral = literal.Substring(0, i);
                
                //don't convert into ternary to avoid Type coercion
                if (hasDecimal || hasExponent) 
                    value = double.Parse(numLiteral);
                else 
                    value = int.Parse(numLiteral);
            }
            else if (literal.StartsWith("true") && (literal.Length == 4 || !IsValidVarNameChar(literal.GetChar(4))))
            {
                value = true;
            }
            else if (literal.StartsWith("false") && (literal.Length == 5 || !IsValidVarNameChar(literal.GetChar(5))))
            {
                value = false;
            }
            else if (literal.StartsWith("null") && (literal.Length == 4 || !IsValidVarNameChar(literal.GetChar(4))))
            {
                value = NullValue.Instance;
            }
            else
            {
                var i = 1;
                var c = (char)0;
                var isExpression = false;
                while (i < literal.Length && IsValidVarNameChar(c = literal.GetChar(i)) || (isExpression = (c == '.' || c == '(' || c == '[')))
                {
                    if (isExpression)
                    {
                        cmd = literal.ParseCommands().FirstOrDefault();
                        return;
                    }
                    
                    i++;
                }
                    
                name = literal.Subsegment(0, i);
            }
        }
    }
}