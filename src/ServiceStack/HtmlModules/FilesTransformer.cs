#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using ServiceStack.Html;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.HtmlModules;

public class FileTransformerOptions
{
    public List<Func<string,string?>> LineTransformers { get; set; } = new();
    public List<Func<string,string>> FilesTransformers { get; set; } = new();
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
    public static FilesTransformer Defaults(bool debugMode, Action<FilesTransformer>? with=null) => 
        FilesTransformerUtils.Defaults(debugMode, with);
    
    public Dictionary<string, FileTransformerOptions> FileExtensions { get; private set; } = new();

    public FileTransformerOptions? GetExt(string fileExt) => FileExtensions.TryGetValue(fileExt, out var options)
        ? options
        : null;

    public string ReadAll(IVirtualFile file)
    {
        if (!FileExtensions.TryGetValue(file.Extension, out var options))
            return file.ReadAllText();
        
        string? line = null;
        var sb = StringBuilderCache.Allocate();
        using var reader = file.OpenText();
        while ((line = reader.ReadLine()) != null)
        {
            foreach (var lineTransformer in options.LineTransformers)
            {
                line = lineTransformer(line!);
                if (line == null) break;
            }
            if (line != null)
                sb.AppendLine(line);
        }

        var fileContents = sb.ToString();
        foreach (var filesTransformer in options.FilesTransformers)
        {
            fileContents = filesTransformer(fileContents);
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
    
}

public static class Lines
{
    public static Func<string, string?> RemoveNotStartingWith(string linePrefix, bool ignoreWhiteSpace=false) => line => ignoreWhiteSpace 
        ? !line.AsSpan().TrimStart().StartsWith(linePrefix) ? line : null
        : !line.StartsWith(linePrefix) ? line : null;

    public static Func<string, string?> RemoveOnlyWhitespace() =>
        line => line.AsSpan().Trim().Length == 0 ? null : line;
}

public static class FilesTransformerUtils
{
    public static FilesTransformer Defaults(bool debugMode = true, Action<FilesTransformer>? with=null)
    {
        var options = new FilesTransformer
        {
            FileExtensions =
            {
                ["html"] = new FileTransformerOptions
                {
                    LineTransformers =
                    {
                        Lines.RemoveNotStartingWith("<!---:", ignoreWhiteSpace: true),
                        Lines.RemoveOnlyWhitespace(),
                    },
                },
                ["js"] = new FileTransformerOptions
                {
                    LineTransformers =
                    {
                        Lines.RemoveNotStartingWith("import "),
                        Lines.RemoveNotStartingWith("declare "),
                        Lines.RemoveNotStartingWith("/**:", ignoreWhiteSpace: true),
                        Lines.RemoveOnlyWhitespace(),
                    }
                },
                ["css"] = new FileTransformerOptions
                {
                    LineTransformers =
                    {
                        Lines.RemoveNotStartingWith("/**:", ignoreWhiteSpace: true),
                        Lines.RemoveOnlyWhitespace(),
                    }
                },
            },
        };
        // TODO test
        // if (!debugMode)
        //     options = options.Minify(Html.Minify.JavaScript | Html.Minify.HtmlAdvanced);
        
        with?.Invoke(options);
        return options;
    }
    
    public static FilesTransformer Minify(this FilesTransformer options, Minify minify) => options.Clone(with: o => {
        if (minify.HasFlag(Html.Minify.JavaScript))
            o.GetExt("js")?.FilesTransformers.Add(Minifiers.JavaScript.Compress);
        if (minify.HasFlag(Html.Minify.Css))
            o.GetExt("css")?.FilesTransformers.Add(Minifiers.Css.Compress);
        if (minify.HasFlag(Html.Minify.HtmlAdvanced))
            o.GetExt("html")?.FilesTransformers.Add(Minifiers.HtmlAdvanced.Compress);
        if (minify.HasFlag(Html.Minify.Html))
            o.GetExt("html")?.FilesTransformers.Add(Minifiers.Html.Compress);
    });
}
    