#nullable enable
using System;
using System.Collections.Generic;
using ServiceStack.Text;

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

public class RemoveBlock : HtmlModuleBlock
{
    public RemoveBlock(string startTag, string endTag, Run behaviour = Run.Always) : base(startTag, endTag, behaviour) {}
    public override string? Transform(string block) => null;
}

public class MinifyBlock : HtmlModuleBlock
{
    public ICompressor Compressor { get; }
    
    public Func<string, string?>? Convert { get; set; }

    public List<HtmlModuleLine> LineTransformers { get; set; } = new();

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
        if (LineTransformers.Count > 0)
        {
            var blockChars = block.AsMemory();
            int startIndex = 0;
            var sb = StringBuilderCache.Allocate();
            while (blockChars.TryReadLine(out var line, ref startIndex))
            {
                foreach (var lineTransformer in LineTransformers)
                {
                    line = lineTransformer.Transform(line);
                    if (line.Length == 0)
                        break;
                }
                if (line.Length > 0)
                {
                    sb.AppendLine(line);
                }
            }
            block = StringBuilderCache.ReturnAndFree(sb);
        }
        
        var output = Compressor.Compress(block);
        return Convert != null
            ? Convert(output)
            : output;
    }
}