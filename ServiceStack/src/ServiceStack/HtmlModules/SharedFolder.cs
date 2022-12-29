#nullable enable
using System;
using System.IO;
using System.Linq;
using ServiceStack.IO;
using ServiceStack.Script;
using ServiceStack.Text;

namespace ServiceStack.HtmlModules;

/// <summary>
/// Register a shared folder to easily import shared .html components or files  
/// &lt;!--shared:Brand,Input--&gt; or /*shared:app.css*/
/// </summary>
public class SharedFolder : IHtmlModulesHandler
{
    public string Name { get; }
    public string SharedDir { get; set; }
    public string DefaultExt { get; }
    public SharedFolder(string name, string sharedDir, string defaultExt)
    {
        if (string.IsNullOrEmpty(defaultExt))
            throw new ArgumentNullException(nameof(defaultExt));
        if (defaultExt[0] != '.')
            throw new ArgumentNullException(nameof(defaultExt) + " file extension must start with '.'");

        Name = name;
        SharedDir = sharedDir;
        DefaultExt = defaultExt;
    }

    public ReadOnlyMemory<byte> Execute(HtmlModuleContext ctx, string files)
    {
        return ctx.Cache($"{Name}:{SharedDir}/{files}", _ => {
            var sb = StringBuilderCache.Allocate();
            if (!ctx.DebugMode) sb.AppendLine(); // Force ASI when concatenating scripts with advanced minifiers
            var paths = files.Split(',').Map(file =>
                SharedDir.CombineWith(file + (file.IndexOf('.') == -1 ? DefaultExt : "")));

            foreach (var path in paths)
            {
                if (path.IndexOf('*') >= 0)
                {
                    var files = ctx.VirtualFiles.GetAllMatchingFiles(path)
                        .OrderBy(file => file.VirtualPath).ToList();
                    foreach (var file in files)
                    {
                        sb.AppendLine(ctx.FileContentsResolver(file));
                    }
                }
                else
                {
                    var file = ctx.VirtualFiles.GetFile(path);
                    if (file == null)
                        throw new FileNotFoundException($"File '{path}' does not exist in {ctx.VirtualFiles}", path);
                    
                    sb.AppendLine(ctx.FileContentsResolver(file));
                }
            }

            return StringBuilderCache.ReturnAndFree(sb).AsMemory().ToUtf8();
        });
    }
}