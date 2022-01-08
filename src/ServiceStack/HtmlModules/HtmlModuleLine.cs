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
    public string[] LinePrefixes { get; }
    public bool IgnoreWhiteSpace { get; }
    
    public RemoveLineStartingWith(string linePrefix, bool ignoreWhiteSpace=false, Run behaviour=Run.Always)
        : this(new[]{ linePrefix }, ignoreWhiteSpace, behaviour) {}
    
    public RemoveLineStartingWith(string[] linePrefixes, bool ignoreWhiteSpace=false, Run behaviour=Run.Always)
    {
        LinePrefixes = linePrefixes;
        IgnoreWhiteSpace = ignoreWhiteSpace;
        Behaviour = behaviour;
    }

    public override string? Transform(string line)
    {
        foreach (var linePrefix in LinePrefixes)
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

public class RemoveLineWithOnlyWhitespace : HtmlModuleLine
{
    public RemoveLineWithOnlyWhitespace(Run behaviour = Run.Always) => Behaviour = behaviour;
    public override string? Transform(string line) => line.AsSpan().Trim().Length == 0 ? null : line;
}
