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
    public ReadOnlyMemory<byte> Execute(HtmlModuleContext ctx, string path)
    {
        return ctx.Cache($"{Name}:{path}", _ => ctx.FileContentsResolver(ctx.AssertFile(path.StartsWith("/")
            ? path
            : ctx.Module.DirPath.CombineWith(path))).AsMemory().ToUtf8());
    }
}
