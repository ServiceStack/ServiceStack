#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.HtmlModules;

public class FileTransformerOptions
{
    public List<HtmlModuleLine> LineTransformers { get; set; } = new();
    public List<HtmlModuleBlock> BlockTransformers { get; set; } = new();
    public List<HtmlModuleBlock> FilesTransformers { get; set; } = new();

    public FileTransformerOptions Without(Run behavior)
    {
        return new FileTransformerOptions
        {
            LineTransformers = LineTransformers.Where(x => x.Behaviour != behavior).ToList(),
            BlockTransformers = BlockTransformers.Where(x => x.Behaviour != behavior).ToList(),
            FilesTransformers = FilesTransformers.Where(x => x.Behaviour != behavior).ToList(),
        };
    }
}

public class FilesTransformer
{
    /// <summary>
    /// Apply no file transformations
    /// </summary>
    public static FilesTransformer None => new();
    
    /// <summary>
    /// Default File Transformer options:
    ///   .html:
    ///     - removes line comments &lt;!---: ... --&gt;
    ///     - removes empty whitespace lines
    ///     - minifies in !DebugMode with <see cref="Minifiers.HtmlAdvanced"/>
    ///   .js:
    ///     - removes lines starting with: 'import ', 'declare '
    ///     - removes line comments /**: ... */
    ///     - removes empty whitespace lines 
    ///     - minifies in !DebugMode with <see cref="Minifiers.JavaScript"/>
    ///   .css:
    ///     - removes line comments /**: ... */
    ///     - removes empty whitespace lines 
    /// </summary>
    public static FilesTransformer Default => Defaults(HostContext.DebugMode);
    public static FilesTransformer Defaults(bool? debugMode = null, Action<FilesTransformer>? with=null)
    {
        var defaults = FilesTransformerUtils.Defaults(with);
        return debugMode == null 
            ? defaults 
            : defaults.Without(debugMode.Value ? Run.IgnoreInDebug : Run.OnlyInDebug);
    }

    public FilesTransformer Without(Run behaviour)
    {
        var to = new FilesTransformer();
        foreach (var entry in this.FileExtensions)
        {
            to.FileExtensions[entry.Key] = this.FileExtensions[entry.Key].Without(behaviour);
        }
        return to;
    }

    public Dictionary<string, FileTransformerOptions> FileExtensions { get; private set; } = new();

    public FileTransformerOptions? GetExt(string fileExt) => FileExtensions.TryGetValue(fileExt, out var options)
        ? options
        : null;

    public string ReadAll(IVirtualFile file)
    {
        if (file == null)
            throw new ArgumentNullException(nameof(file));
        
        if (!FileExtensions.TryGetValue(file.Extension, out var extOptions))
            return file.ReadAllText();

        var options = extOptions.Without(HostContext.DebugMode ? Run.IgnoreInDebug : Run.OnlyInDebug);
        
        string? line;
        var sb = StringBuilderCache.Allocate();
        using var reader = file.OpenText();
        HtmlModuleBlock? inBlock = null;
        var blockLines = new List<string>();
        while ((line = reader.ReadLine()) != null)
        {
            var lineSpan = line.AsMemory();
            var trimmedLine = lineSpan.Span.Trim();
            if (inBlock == null)
            {
                foreach (var lineTransformer in options.LineTransformers)
                {
                    lineSpan = lineTransformer.Transform(lineSpan);
                    if (lineSpan.Length == 0)
                    {
                        line = null;
                        break;
                    }
                    line = lineSpan.ToString();
                }
            }
            else
            {
                HtmlModuleBlock? endBlock = null;
                foreach (var x in options.BlockTransformers)
                {
                    if (trimmedLine.EqualTo(x.EndTag))
                    {
                        endBlock = x;
                        break;
                    }
                }
                if (endBlock != null)
                {
                    var blockOutput = endBlock.Transform(blockLines);
                    if (blockOutput != null)
                    {
                        sb.AppendLine(blockOutput);
                    }
                    inBlock = null;
                    blockLines.Clear();
                    continue;
                }
                blockLines.Add(line);
                continue;
            }
            if (line == null)
                continue;

            HtmlModuleBlock? startBlock = null;
            foreach (var x in options.BlockTransformers)
            {
                if (trimmedLine.EqualTo(x.StartTag))
                {
                    startBlock = x;
                    break;
                }
            }
            if (startBlock != null)
            {
                inBlock = startBlock;
                continue;
            }

            sb.AppendLine(line);
        }

        var fileContents = sb.ToString();
        foreach (var filesTransformer in options.FilesTransformers)
        {
            fileContents = filesTransformer.Transform(fileContents);
            if (fileContents == null)
                return string.Empty;
        }
        return fileContents;
    }

    public FilesTransformer Clone(Action<FilesTransformer>? with = null)
    {
        var clone = new FilesTransformer
        {
            FileExtensions = new Dictionary<string, FileTransformerOptions>(FileExtensions)
        };
        with?.Invoke(clone);
        return clone;
    }

    public static void RecreateDirectory(string dirPath, int timeoutMs = 1000) =>
        FileSystemVirtualFiles.RecreateDirectory(dirPath, timeoutMs);
    
    public void CopyAll(FileSystemVirtualFiles source, FileSystemVirtualFiles target,
        bool cleanTarget = false,
        Func<IVirtualFile, bool>? ignore = null, 
        Action<IVirtualFile, string>? afterCopy = null)
    {
        if (cleanTarget)
            FileSystemVirtualFiles.RecreateDirectory(target.RootDirInfo.FullName);

        foreach (var file in source.GetAllFiles())
        {
            if (ignore != null && ignore(file)) 
                continue;

            var contents = ReadAll(file);
            target.WriteFile(file.VirtualPath, contents);
            afterCopy?.Invoke(file, contents);
        }
    }
    
    public static List<HtmlModuleLine> HtmlLineTransformers { get; } = new()
    {
        // Enable static typing during dev, strip from browser to run
        new RemoveLineStartingWith(new[]{ "import ", "declare " }, ignoreWhiteSpace:false, Run.Always), 
        new RemovePrefixesFromLine("export ", ignoreWhiteSpace:false, Run.Always), 
        new RemoveLineStartingWith("/** @type", ignoreWhiteSpace:true, behaviour:Run.Always),
        new RemoveLineStartingWith("@type", ignoreWhiteSpace:true, behaviour:Run.Always),
        new RemoveLineStartingWith("/** @param", ignoreWhiteSpace:true, behaviour:Run.Always),
        new RemoveLineStartingWith("*  @param", ignoreWhiteSpace:true, behaviour:Run.Always),
        new RemoveLineStartingWith("@param", ignoreWhiteSpace:true, behaviour:Run.Always),
        new RemoveLineStartingWith("/** @return", ignoreWhiteSpace:true, behaviour:Run.Always),
        new RemoveLineStartingWith("@return", ignoreWhiteSpace:true, behaviour:Run.Always),
        new RemoveLineEndingWith(new[]{ "/*debug*/", "<!--debug-->" }, ignoreWhiteSpace:true, Run.IgnoreInDebug),
        // Hide dev comments from browser
        new RemoveLineStartingWith("<!---:", ignoreWhiteSpace:true, Run.Always),
        new RemoveLineStartingWith("/**:", ignoreWhiteSpace:true, behaviour:Run.Always),
        new RemoveLineWithOnlyWhitespace(Run.Always),
    };

    // Enable static typing during dev, strip from browser to run
    public static List<HtmlModuleLine> JsLineTransformers { get; } = new()
    {
        new RemoveLineStartingWith(new[] { "import ", "declare " }, ignoreWhiteSpace:false, Run.Always),
        new RemovePrefixesFromLine("export ", ignoreWhiteSpace:false, Run.Always),
        new RemoveLineStartingWith("/** @type", ignoreWhiteSpace:true, behaviour:Run.Always),
        new RemoveLineStartingWith("@type", ignoreWhiteSpace:true, behaviour:Run.Always),
        new RemoveLineStartingWith("/** @param", ignoreWhiteSpace:true, behaviour:Run.Always),
        new RemoveLineStartingWith("*  @param", ignoreWhiteSpace:true, behaviour:Run.Always),
        new RemoveLineStartingWith("@param", ignoreWhiteSpace:true, behaviour:Run.Always),
        new RemoveLineStartingWith("/** @return", ignoreWhiteSpace:true, behaviour:Run.Always),
        new RemoveLineStartingWith("@return", ignoreWhiteSpace:true, behaviour:Run.Always),
        new RemoveLineEndingWith("/*debug*/", ignoreWhiteSpace:true, Run.IgnoreInDebug),
        // Hide dev comments from browser
        new RemoveLineStartingWith("/**:", ignoreWhiteSpace:true, behaviour:Run.Always),
        new RemoveLineWithOnlyWhitespace(Run.Always),
    };

    public static List<HtmlModuleLine> CssLineTransformers { get; } = new()
    {
        new RemoveLineStartingWith("/**:", ignoreWhiteSpace: true, Run.Always),
        new RemoveLineWithOnlyWhitespace(Run.Always),
        new RemoveLineEndingWith("/*debug*/", ignoreWhiteSpace: true, Run.IgnoreInDebug),
    };
}

public static class FilesTransformerUtils
{
    public static FilesTransformer Defaults(Action<FilesTransformer>? with=null)
    {
        var options = new FilesTransformer
        {
            FileExtensions =
            {
                ["html"] = new FileTransformerOptions
                {
                    BlockTransformers = {
                        new RawBlock("<!--raw-->", "<!--/raw-->", Run.Always),
                        new MinifyBlock("<!--minify-->", "<!--/minify-->", Minifiers.HtmlAdvanced, Run.IgnoreInDebug),
                        new MinifyBlock("<script minify>", "</script>", Minifiers.JavaScript, Run.IgnoreInDebug) {
                            LineTransformers = FilesTransformer.JsLineTransformers.ToList(),
                            Convert = js => "<script>" + js + "</script>",
                        },
                        new MinifyBlock("<style minify>", "</style>", Minifiers.Css, Run.IgnoreInDebug) {
                            Convert = css => "<style>" + css + "</style>"
                        },
                    },
                    LineTransformers = FilesTransformer.HtmlLineTransformers.ToList(),
                },
                ["js"] = new FileTransformerOptions
                {
                    BlockTransformers = {
                        new RawBlock("/*raw:*/", "/*:raw*/", Run.Always),
                        new MinifyBlock("/*minify:*/", "/*:minify*/", Minifiers.JavaScript, Run.IgnoreInDebug) {
                            LineTransformers = FilesTransformer.JsLineTransformers.ToList()
                        },
                    },
                    LineTransformers = FilesTransformer.JsLineTransformers.ToList(),
                },
                ["css"] = new FileTransformerOptions
                {
                    BlockTransformers = {
                        new RawBlock("/*raw:*/", "/*:raw*/", Run.Always),
                        new MinifyBlock("/*minify:*/", "/*:minify*/", Minifiers.Css, Run.IgnoreInDebug),
                    },
                    LineTransformers = FilesTransformer.CssLineTransformers.ToList(),
                },
            },
        };
        
        with?.Invoke(options);
        return options;
    }
    
    public static FilesTransformer Minify(this FilesTransformer options, Minify minify, Run behavior = Run.OnlyInDebug) => options.Clone(with: o => {
        if (minify.HasFlag(Html.Minify.JavaScript))
            o.GetExt("js")?.FilesTransformers.Add(new MinifyBlock(Minifiers.JavaScript, behavior));
        if (minify.HasFlag(Html.Minify.Css))
            o.GetExt("css")?.FilesTransformers.Add(new MinifyBlock(Minifiers.Css, behavior));
        if (minify.HasFlag(Html.Minify.HtmlAdvanced))
            o.GetExt("html")?.FilesTransformers.Add(new MinifyBlock(Minifiers.HtmlAdvanced, behavior));
        if (minify.HasFlag(Html.Minify.Html))
            o.GetExt("html")?.FilesTransformers.Add(new MinifyBlock(Minifiers.Html, behavior));
    });
}
    