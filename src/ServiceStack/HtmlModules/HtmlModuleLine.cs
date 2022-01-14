using System;
using ServiceStack.Text;

#nullable enable

namespace ServiceStack.HtmlModules;

public abstract class HtmlModuleLine
{
    public Run Behaviour { get; set; }
    public abstract string? Transform(string line);
}

public class RemoveLineStartingWith : HtmlModuleLine
{
    public string[] Prefixes { get; }
    public bool IgnoreWhiteSpace { get; }
    
    public RemoveLineStartingWith(string prefix, bool ignoreWhiteSpace=false, Run behaviour=Run.Always)
        : this(new[]{ prefix }, ignoreWhiteSpace, behaviour) {}
    
    public RemoveLineStartingWith(string[] prefixes, bool ignoreWhiteSpace=false, Run behaviour=Run.Always)
    {
        Prefixes = prefixes;
        IgnoreWhiteSpace = ignoreWhiteSpace;
        Behaviour = behaviour;
    }

    public override string? Transform(string line)
    {
        foreach (var linePrefix in Prefixes)
        {
            if (IgnoreWhiteSpace)
            {
                if (line.AsSpan().TrimStart().StartsWith(linePrefix))
                    return null;
            }
            else
            {
                if (line.StartsWith(linePrefix))
                    return null;
            }
        }
        return line;
    }
}

public class RemovePrefixesFromLine : HtmlModuleLine
{
    public string[] Prefixes { get; }
    public bool IgnoreWhiteSpace { get; }
    
    public RemovePrefixesFromLine(string prefix, bool ignoreWhiteSpace=false, Run behaviour=Run.Always)
        : this(new[]{ prefix }, ignoreWhiteSpace, behaviour) {}
    
    public RemovePrefixesFromLine(string[] prefixes, bool ignoreWhiteSpace=false, Run behaviour=Run.Always)
    {
        Prefixes = prefixes;
        IgnoreWhiteSpace = ignoreWhiteSpace;
        Behaviour = behaviour;
    }

    public override string? Transform(string line)
    {
        foreach (var linePrefix in Prefixes)
        {
            if (IgnoreWhiteSpace)
            {
                if (line.AsSpan().TrimStart().StartsWith(linePrefix))
                    return line.Substring(line.IndexOf(linePrefix, StringComparison.Ordinal) + linePrefix.Length);
            }
            else
            {
                if (line.StartsWith(linePrefix))
                    return line.Substring(linePrefix.Length);
            }
        }
        return line;
    }
}

public class RemoveLineWithOnlyWhitespace : HtmlModuleLine
{
    public RemoveLineWithOnlyWhitespace(Run behaviour = Run.Always) => Behaviour = behaviour;
    public override string? Transform(string line) => line.AsSpan().Trim().Length == 0 ? null : line;
}
