#nullable enable
using System;
using System.Collections.Generic;

namespace ServiceStack.HtmlModules;

public abstract class HtmlModuleBlock
{
    public Run Behaviour { get; set; }
    public string StartTag { get; }
    public string EndTag { get; }

    /// <summary>
    /// When tags are not used, e.g. in File Transformers
    /// </summary>
    protected HtmlModuleBlock(Run behaviour)
    {
        Behaviour = behaviour;
        this.StartTag = this.EndTag = "NOT_FOR_BLOCK_TRANSFORMERS";
    }

    protected HtmlModuleBlock(string startTag, string endTag, Run behaviour = Run.Always)
    {
        StartTag = startTag;
        EndTag = endTag;
        Behaviour = behaviour;
    }

    public virtual string? Transform(List<string> lines) => 
        Transform(string.Join(Environment.NewLine, lines));

    public virtual string? Transform(string block) => block;
}

public class RawBlock : HtmlModuleBlock
{
    public RawBlock(string startTag, string endTag, Run behaviour = Run.Always) : base(startTag, endTag, behaviour) {}
}

public class MinifyBlock : HtmlModuleBlock
{
    public ICompressor Compressor { get; }

    /// <summary>
    /// When tags are not used, e.g. in File Transformers
    /// </summary>
    public MinifyBlock(ICompressor compressor, Run behaviour = Run.Always) : base(behaviour) => Compressor = compressor;

    public MinifyBlock(string startTag, string endTag, ICompressor compressor, Run behaviour = Run.IgnoreInDebug) 
        : base(startTag, endTag, behaviour)
    {
        Compressor = compressor;
    }

    public override string? Transform(string block)
    {
        var output = Compressor.Compress(block);
        return output;
    }
}