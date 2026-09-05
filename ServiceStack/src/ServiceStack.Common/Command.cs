using System;
using System.Collections.Generic;
using ServiceStack.Text;

#if !NET6_0_OR_GREATER
using ServiceStack.Extensions;
#endif

namespace ServiceStack;

public class Command
{
    public string Name { get; set; }

    public ReadOnlyMemory<char> Original { get; set; }

    public List<ReadOnlyMemory<char>> Args { get; set; } = new();

    public ReadOnlyMemory<char> Suffix { get; set; }

    public int IndexOfMethodEnd(ReadOnlyMemory<char> commandsString, int pos)
    {
        //finding end of suffix, e.g: 'SUM(*) Total', 'SUM(*) as total_count', or 'SUM(*) AS "Total Count"'
        var endPos = pos;
        var cmdSpan = commandsString.Span;
        while (cmdSpan.Length > endPos && char.IsWhiteSpace(cmdSpan[endPos]))
            endPos++;

        if (cmdSpan.Length >= endPos + 2 && cmdSpan.Slice(endPos, 2).Equals("as".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            if (cmdSpan.Length == endPos + 2 || char.IsWhiteSpace(cmdSpan[endPos + 2]))
            {
                endPos += 2;
                while (cmdSpan.Length > endPos && char.IsWhiteSpace(cmdSpan[endPos]))
                    endPos++;
            }
        }

        if (cmdSpan.Length > endPos)
        {
            var c = cmdSpan[endPos];
            if (c is '"' or '\'' or '`')
            {
                endPos++;
                while (cmdSpan.Length > endPos && cmdSpan[endPos] != c)
                    endPos++;
                if (cmdSpan.Length > endPos && cmdSpan[endPos] == c)
                    endPos++;
            }
            else if (c == '[')
            {
                endPos++;
                while (cmdSpan.Length > endPos && cmdSpan[endPos] != ']')
                    endPos++;
                if (cmdSpan.Length > endPos && cmdSpan[endPos] == ']')
                    endPos++;
            }
            else
            {
                while (cmdSpan.Length > endPos &&
                       (char.IsLetterOrDigit(cmdSpan[endPos]) || cmdSpan[endPos] is '_' or '$'))
                    endPos++;
            }
        }

        this.Suffix = commandsString.Slice(pos, endPos - pos).TrimEnd();

        return endPos;
    }

    //Output different format for debugging to verify command was parsed correctly
    public virtual string ToDebugString()
    {
        var sb = StringBuilderCacheAlt.Allocate();
        if (Args != null)
        {
            foreach (var arg in Args)
            {
                if (sb.Length > 0)
                    sb.Append('|');
                sb.Append(arg);
            }
        }

        return $"[{Name}:{StringBuilderCacheAlt.ReturnAndFree(sb)}]{Suffix}";
    }

    public override string ToString()
    {
        var sb = StringBuilderCacheAlt.Allocate();
        if (Args != null)
        {
            foreach (var arg in Args)
            {
                if (sb.Length > 0)
                    sb.Append(',');
                sb.Append(arg);
            }
        }

        return $"{Name}({StringBuilderCacheAlt.ReturnAndFree(sb)}){Suffix}";
    }

    public ReadOnlyMemory<char> AsMemory() => ToString().AsMemory();
}