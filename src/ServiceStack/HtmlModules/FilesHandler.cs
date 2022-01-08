#nullable enable
using System;
using System.Linq;
using ServiceStack.Text;

namespace ServiceStack.HtmlModules;

/// <summary>
/// Include files matching a glob pattern using absolute or module relative paths, e.g  
/// &lt;!--files:components/*.html--&gt; or /*files:components/*.css*/
/// &lt;!--files:/dir/components/*.html--&gt; or /*files:/dir/*.css*/
/// </summary>
public class FilesHandler : IHtmlModulesHandler
{
    public string Name { get; }
    public FilesHandler(string name) => Name = name;
    public ReadOnlyMemory<byte> Execute(HtmlModuleContext ctx, string paths)
    {
        return ctx.Cache($"{Name}:{paths}", _ => {
            var sb = StringBuilderCache.Allocate();
            var usePath = paths.StartsWith("/")
                ? paths
                : ctx.Module.DirPath.CombineWith(paths);
            var sortedFiles = ctx.VirtualFiles.GetAllMatchingFiles(usePath)
                .OrderBy(file => file.VirtualPath).ToList();
            foreach (var file in sortedFiles)
            {
                sb.AppendLine(ctx.FileContentsResolver(file));
            }
            return StringBuilderCache.ReturnAndFree(sb).AsMemory().ToUtf8();
        });
    }
}