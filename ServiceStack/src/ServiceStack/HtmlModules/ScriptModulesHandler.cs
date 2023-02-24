#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.HtmlModules;

/// <summary>
/// Include script modules matching a glob pattern using absolute or module relative paths, e.g  
/// &lt;!--modules:components/*.mjs--&gt;
/// </summary>
public class ScriptModulesHandler : IHtmlModulesHandler
{
    public string Name { get; }
    public ScriptModulesHandler(string name) => Name = name;
    public Func<HtmlModuleContext,IVirtualPathProvider>? VirtualFilesResolver { get; set; }

    public Func<string, string> NormalizeVirtualPath { get; set; } = DefaultNormalizeVirtualPath;

    // Resource VFS replaces '-' with '_' 
    public static Dictionary<string, string> NormalizedPaths { get; set; } = new()
    {
        ["modules/admin_ui"] = "modules/admin-ui"
    };

    public static string DefaultNormalizeVirtualPath(string path)
    {
        foreach (var entry in NormalizedPaths)
        {
            if (path.StartsWith(entry.Key))
                return entry.Value + path.Substring(entry.Key.Length);
        }
        return path;
    }

    public ReadOnlyMemory<byte> Execute(HtmlModuleContext ctx, string paths)
    {
        return ctx.Cache($"{Name}:{ctx.Module.DirPath}:{paths}", _ => {
            var useVfs = VirtualFilesResolver?.Invoke(ctx) ?? ctx.VirtualFiles;
            var sb = StringBuilderCache.Allocate();
            var usePath = paths.StartsWith("/")
                ? paths
                : ctx.Module.DirPath.CombineWith(paths);

            var allFiles = useVfs.GetAllMatchingFiles(usePath).ToList();

            // Remove duplicates (from overriding), first file wins
            var uniqueFiles = new List<IVirtualFile>();
            var existingFiles = new HashSet<string>(); 
            foreach (var file in allFiles)
            {
                var path = NormalizeVirtualPath(file.VirtualPath);
                if (existingFiles.Contains(path))
                    continue;
                existingFiles.Add(path);
                uniqueFiles.Add(file);
            }
            
            var sortedFiles = uniqueFiles
                .OrderBy(file => file.VirtualPath).ToList();
            foreach (var file in sortedFiles)
            {
                sb.AppendLine("<script type=\"module\">");
                sb.AppendLine(ctx.FileContentsResolver(file));
                sb.AppendLine("</script>");
            }
            return StringBuilderCache.ReturnAndFree(sb).AsMemory().ToUtf8();
        });
    }
}