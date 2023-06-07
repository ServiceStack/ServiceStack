#nullable enable
using System;
using System.IO;
using System.Linq;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.HtmlModules;

/// <summary>
/// Register a shared folder to easily import shared .html components or files  
/// &lt;!--shared:custom-meta--&gt; or /*shared:app.css*/
/// </summary>
public class SharedFolder : IHtmlModulesHandler
{
    public string Name { get; }
    public string SharedDir { get; set; }
    public string DefaultExt { get; }
    public Func<IVirtualFile, string>? Header { get; set; }
    public Func<IVirtualFile, string>? Footer { get; set; }
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
                        if (Header != null) sb.AppendLine(Header(file));
                        sb.AppendLine(ctx.FileContentsResolver(file));
                        if (Footer != null) sb.AppendLine(Footer(file));
                    }
                }
                else
                {
                    var file = ctx.VirtualFiles.GetFile(path);
                    if (file == null)
                        throw new FileNotFoundException($"File '{path}' does not exist in {ctx.VirtualFiles}", path);
                    
                    if (Header != null) sb.AppendLine(Header(file));
                    sb.AppendLine(ctx.FileContentsResolver(file));
                    if (Footer != null) sb.AppendLine(Footer(file));
                }
            }

            return StringBuilderCache.ReturnAndFree(sb).AsMemory().ToUtf8();
        });
    }
}