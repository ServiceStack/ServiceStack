#nullable enable
using System;
using ServiceStack.IO;
using ServiceStack.Text;

namespace ServiceStack.HtmlModules;

/// <summary>
/// Include a single file using absolute or module relative path, e.g  
/// &lt;!--file:single.html--&gt; or /*file:single.txt*/
/// &lt;!--file:/path/to/single.html--&gt; or /*file:/path/to/single.txt*/
/// </summary>
public class FileHandler : IHtmlModulesHandler
{
    public string Name { get; }
    public FileHandler(string name) => Name = name;
    public Func<HtmlModuleContext,IVirtualPathProvider>? VirtualFilesResolver { get; set; }
    public ReadOnlyMemory<byte> Execute(HtmlModuleContext ctx, string path)
    {
        return ctx.Cache($"{Name}:{ctx.Module.DirPath}:{path}", _ =>
        {
            var useVfs = VirtualFilesResolver?.Invoke(ctx) ?? ctx.VirtualFiles;
            var content = ctx.FileContentsResolver(ctx.AssertFile(useVfs, path.StartsWith("/")
                ? path
                : ctx.Module.DirPath.CombineWith(path)));
            return content.AsMemory().ToUtf8();
        });
    }
}
