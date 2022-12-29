using System;
using ServiceStack.Text;

#nullable enable

namespace ServiceStack.HtmlModules;

public abstract class HtmlModuleLine
{
    public Run Behaviour { get; set; }
    public abstract ReadOnlyMemory<char> Transform(ReadOnlyMemory<char> line);
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

    public override ReadOnlyMemory<char> Transform(ReadOnlyMemory<char> line)
    {
        foreach (var linePrefix in Prefixes)
        {
            if (IgnoreWhiteSpace)
            {
                if (line.Span.TrimStart().StartsWith(linePrefix))
                    return default;
            }
            else
            {
                if (line.StartsWith(linePrefix))
                    return default;
            }
        }
        return line;
    }
}

public class RemoveLineEndingWith : HtmlModuleLine
{
    public string[] Suffixes { get; }
    public bool IgnoreWhiteSpace { get; }
    
    public RemoveLineEndingWith(string suffix, bool ignoreWhiteSpace=false, Run behaviour=Run.Always)
        : this(new[]{ suffix }, ignoreWhiteSpace, behaviour) {}
    
    public RemoveLineEndingWith(string[] prefixes, bool ignoreWhiteSpace=false, Run behaviour=Run.Always)
    {
        Suffixes = prefixes;
        IgnoreWhiteSpace = ignoreWhiteSpace;
        Behaviour = behaviour;
    }

    public override ReadOnlyMemory<char> Transform(ReadOnlyMemory<char> line)
    {
        foreach (var linePrefix in Suffixes)
        {
            if (IgnoreWhiteSpace)
            {
                if (line.Span.TrimStart().EndsWith(linePrefix))
                    return default;
            }
            else
            {
                if (line.EndsWith(linePrefix))
                    return default;
            }
        }
        return line;
    }
}

public class RemoveLineContaining : HtmlModuleLine
{
    public string[] Tokens { get; }
    
    public RemoveLineContaining(string token, Run behaviour=Run.Always)
        : this(new[]{ token }, behaviour) {}
    
    public RemoveLineContaining(string[] tokens, Run behaviour=Run.Always)
    {
        Tokens = tokens;
        Behaviour = behaviour;
    }

    public override ReadOnlyMemory<char> Transform(ReadOnlyMemory<char> line)
    {
        foreach (var token in Tokens)
        {
            if (line.IndexOf(token) >= 0)
                return default;
        }
        return line;
    }
}

public class ApplyToLineContaining : HtmlModuleLine
{
    public string Token { get; }
    private Func<ReadOnlyMemory<char>, ReadOnlyMemory<char>> Fn { get; }
    public ApplyToLineContaining(string token, Func<ReadOnlyMemory<char>, ReadOnlyMemory<char>> fn, Run behaviour=Run.Always)
    {
        Token = token;
        Fn = fn;
        Behaviour = behaviour;
    }
    public override ReadOnlyMemory<char> Transform(ReadOnlyMemory<char> line) => line.IndexOf(Token) >= 0 ? Fn(line) : line;
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

    public override ReadOnlyMemory<char> Transform(ReadOnlyMemory<char> line)
    {
        foreach (var linePrefix in Prefixes)
        {
            if (IgnoreWhiteSpace)
            {
                if (line.Span.TrimStart().StartsWith(linePrefix))
                    return line.Slice(line.IndexOf(linePrefix) + linePrefix.Length);
            }
            else
            {
                if (line.StartsWith(linePrefix))
                    return line.Slice(linePrefix.Length);
            }
        }
        return line;
    }
}

public class RemoveLineWithOnlyWhitespace : HtmlModuleLine
{
    public RemoveLineWithOnlyWhitespace(Run behaviour = Run.Always) => Behaviour = behaviour;
    public override ReadOnlyMemory<char> Transform(ReadOnlyMemory<char> line) => line.Span.Trim().Length == 0 ? default : line;
}
