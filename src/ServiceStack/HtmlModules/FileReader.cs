#nullable enable
using System;
using System.Collections.Generic;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.HtmlModules;

public class FileReaderOptions
{
    public List<Func<string,string?>> LineTransformers { get; set; } = new();
    public List<Func<string,string>> FileTransformers { get; set; } = new();
}

public class FileReader
{
    public static void Set(string fileExt, FileReaderOptions options) => 
        FileExtensionReaderOptions[fileExt] = options;

    public static Func<string, string?> NotStartingWith(string linePrefix) =>
        line => !line.StartsWith(linePrefix) ? line : null;

    public static Dictionary<string, FileReaderOptions> FileExtensionReaderOptions { get; } = new()
    {
        ["js"] = new FileReaderOptions {
            LineTransformers = {
                NotStartingWith("import "),
            }
        }
    };

    public static string Read(IVirtualFile file)
    {
        if (!FileExtensionReaderOptions.TryGetValue(file.Extension, out var options))
            return file.ReadAllText();
        
        string? line = null;
        var sb = StringBuilderCache.Allocate();
        using var reader = file.OpenText();
        while ((line = reader.ReadLine()) != null)
        {
            foreach (var lineTransformer in options.LineTransformers)
            {
                line = lineTransformer(line!);
                if (line == null) continue;
                sb.AppendLine(line);
            }
        }

        var fileContents = sb.ToString();
        foreach (var fileTransformer in options.FileTransformers)
        {
            fileContents = fileTransformer(fileContents);
        }
        return fileContents;
    }
}