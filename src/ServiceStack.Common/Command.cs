using System;
using System.Collections.Generic;
using ServiceStack.Text;

namespace ServiceStack
{
    public class Command
    {
        public string Name { get; set; }

        public ReadOnlyMemory<char> Original { get; set; }

        public List<ReadOnlyMemory<char>> Args { get; internal set; } = new List<ReadOnlyMemory<char>>();

        public ReadOnlyMemory<char> Suffix { get; set; }

        public int IndexOfMethodEnd(ReadOnlyMemory<char> commandsString, int pos)
        {
            //finding end of suffix, e.g: 'SUM(*) Total' or 'SUM(*) as Total'
            var endPos = pos;
            var cmdSpan = commandsString.Span;
            while (cmdSpan.Length > endPos && char.IsWhiteSpace(cmdSpan[endPos]))
                endPos++;

            if (cmdSpan.Length > endPos && cmdSpan.IndexOf("as ", endPos) == endPos)
                endPos += "as ".Length;

            while (cmdSpan.Length > endPos && char.IsWhiteSpace(cmdSpan[endPos]))
                endPos++;

            while (cmdSpan.Length > endPos &&
                   char.IsLetterOrDigit(cmdSpan[endPos]))
                endPos++;

            this.Suffix = commandsString.Slice(pos, endPos - pos).TrimEnd();

            return endPos;
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

            return $"[{Name}:{StringBuilderCacheAlt.ReturnAndFree(sb)}]{Suffix}";
        }

        public override string ToString()
        {
            var sb = StringBuilderCacheAlt.Allocate();
            foreach (var arg in Args)
            {
                if (sb.Length > 0)
                    sb.Append(',');
                sb.Append(arg);
            }

            return $"{Name}({StringBuilderCacheAlt.ReturnAndFree(sb)}){Suffix}";
        }

        public ReadOnlyMemory<char> AsMemory() => ToString().AsMemory();
    }
}