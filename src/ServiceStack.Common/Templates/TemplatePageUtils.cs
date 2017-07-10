using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ServiceStack.Text;

#if NETSTANDARD1_3
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public class RawString : IRawString
    {
        private string value;
        public RawString(string value) => this.value = value;
        public string ToRawString() => value;
    }

    public class NullValue : RawString
    {
        public static NullValue Instance = new NullValue();
        private NullValue() : base("null") {}
    }

    public static class TemplatePageUtils
    {
        static readonly char[] VarDelimiters = { '|', '}' };

        public static List<PageFragment> ParseTemplatePage(string text)
        {
            return ParseTemplatePage(new StringSegment(text));
        }

        public static List<PageFragment> ParseTemplatePage(StringSegment text)
        {
            var to = new List<PageFragment>();

            if (text.IsNullOrWhiteSpace())
                return to;
            
            int pos;
            var lastPos = 0;
            while ((pos = text.IndexOf("{{", lastPos)) != -1)
            {
                var block = text.Subsegment(lastPos, pos - lastPos);
                to.Add(new PageStringFragment(block));

                var varStartPos = pos + 2;
                var varEndPos = text.IndexOfAny(VarDelimiters, varStartPos);
                var varName = text.Subsegment(varStartPos, varEndPos - varStartPos).Trim();
                if (varEndPos == -1)
                    throw new ArgumentException($"Invalid Server HTML Template at '{text.SafeSubsegment(50)}...'", nameof(text));

                List<Command> filterCommands = null;
                
                var isFilter = text.GetChar(varEndPos) == '|';
                if (isFilter)
                {
                    filterCommands = text.Subsegment(varEndPos + 1).ParseCommands(
                        separator: '|',
                        atEndIndex: (str, strPos) =>
                        {
                            while (str.Length > strPos && char.IsWhiteSpace(str.GetChar(strPos)))
                                strPos++;

                            if (str.Length > strPos + 1 && str.GetChar(strPos) == '}' && str.GetChar(strPos + 1) == '}')
                            {
                                varEndPos = varEndPos + 1 + strPos + 1;
                                return strPos;
                            }
                            return null;
                        });
                }
                else
                {
                    varEndPos += 1;
                }

                lastPos = varEndPos + 1;
                var originalText = text.Subsegment(pos, lastPos - pos);

                to.Add(new PageVariableFragment(originalText, varName, filterCommands));
            }

            if (lastPos != text.Length)
            {
                var lastBlock = lastPos == 0 ? text : text.Subsegment(lastPos);
                to.Add(new PageStringFragment(lastBlock));
            }

            return to;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRawString ToRawString(this string value) => 
            new RawString(value ?? "");
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IRawString ToRawString(this StringSegment value) => 
            new RawString(value.HasValue ? value.Value : "");
    }
}