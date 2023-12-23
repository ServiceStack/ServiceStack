#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.HtmlModules;

/// <summary>
/// Include files matching a glob pattern using absolute or module relative paths, e.g  
/// &lt;!--files:components/*.html--&gt; or /*files:components/*.css*/
/// &lt;!--files:/dir/components/*.html--&gt; or /*files:/dir/*.css*/
/// </summary>
public class FilesHandler(string name) : IHtmlModulesHandler
{
    public string Name { get; } = name;
    public Func<HtmlModuleContext,IVirtualPathProvider>? VirtualFilesResolver { get; set; }
    public Func<IVirtualFile, string>? Header { get; set; }
    public Func<IVirtualFile, string>? Footer { get; set; }

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
                if (Header != null) sb.AppendLine(Header(file));
                sb.AppendLine(ctx.FileContentsResolver(file));
                if (Footer != null) sb.AppendLine(Footer(file));
            }
            return StringBuilderCache.ReturnAndFree(sb).AsMemory().ToUtf8();
        });
    }
}