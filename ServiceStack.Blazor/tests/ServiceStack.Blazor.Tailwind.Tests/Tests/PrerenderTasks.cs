using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using ServiceStack;
using ServiceStack.IO;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Bunit;
using static System.Console;
using RouteAttribute = Microsoft.AspNetCore.Components.RouteAttribute;

namespace MyApp.Tests;

[TestFixture, Category("prerender"), Explicit]
public class PrerenderTasks
{
    Bunit.TestContext Context;
    string ClientDir;
    string WwrootDir => ClientDir.CombineWith("wwwroot");
    string PrerenderDir => WwrootDir.CombineWith("prerender");

    public PrerenderTasks()
    {
        Context = new();
        var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
        ClientDir = config[nameof(ClientDir)]
            ?? throw new Exception($"{nameof(ClientDir)} not defined in appsettings.json");
        FileSystemVirtualFiles.RecreateDirectory(PrerenderDir);
    }

    void Render<T>(params ComponentParameter[] parameters) where T : IComponent
    {
        WriteLine($"Rendering: {typeof(T).FullName}...");
        var component = Context.RenderComponent<T>(parameters);
        var route = typeof(T).GetCustomAttribute<RouteAttribute>()?.Template;
        if (string.IsNullOrEmpty(route))
            throw new Exception($"Couldn't infer @page for component {typeof(T).Name}");

        var fileName = route.EndsWith("/") ? route + "index.html" : $"{route}.html";

        var writeTo = Path.GetFullPath(PrerenderDir.CombineWith(fileName));
        WriteLine($"Written to {writeTo}");
        File.WriteAllText(writeTo, component.Markup);
    }

    [Test]
    public void PrerenderPages()
    {
        Render<Client.Pages.Index>();
    }

    [Test]
    public async Task PrerenderMarkdown()
    {
        var srcDir = WwrootDir.CombineWith("content").Replace('\\', '/');
        var dstDir = WwrootDir.CombineWith("docs").Replace('\\', '/');

        var indexPage = PageTemplate.Create(WwrootDir.CombineWith("index.html"));
        if (!Directory.Exists(srcDir)) throw new Exception($"{Path.GetFullPath(srcDir)} does not exist");
        FileSystemVirtualFiles.RecreateDirectory(dstDir);

        foreach (var file in new DirectoryInfo(srcDir).GetFiles("*.md", SearchOption.AllDirectories))
        {
            WriteLine($"Converting {file.FullName} ...");

            var name = file.Name.WithoutExtension();
            var docRender = await Client.MarkdownUtils.LoadDocumentAsync(name, doc =>
                Task.FromResult(File.ReadAllText(file.FullName)));

            if (docRender.Failed)
            {
                WriteLine($"Failed: {docRender.ErrorMessage}");
                continue;
            }

            var dirName = dstDir.IndexOf("wwwroot") >= 0
                ? dstDir.LastRightPart("wwwroot").Replace('\\', '/')
                : new DirectoryInfo(dstDir).Name;
            var path = dirName.CombineWith(name == "index" ? "" : name);

            var mdBody = @$"
<div class=""prose lg:prose-xl min-vh-100 m-3"" data-prerender=""{path}"">
    <div class=""markdown-body"">
        {docRender.Response!.Preview!}
    </div>
</div>";
            var prerenderedPage = indexPage.Render(mdBody);
            string htmlPath = Path.GetFullPath(Path.Combine(dstDir, $"{name}.html"));
            File.WriteAllText(htmlPath, prerenderedPage);
            WriteLine($"Written to {htmlPath}");
        }
    }
}


/// <summary>
/// Parses index.html and uses its layout to generate prerendered pages inside <!--PAGE--><!--/PAGE-->
/// </summary>
public class PageTemplate
{
    string? Header { get; set; }
    string? Footer { get; set; }

    public PageTemplate(string? header, string? footer)
    {
        Header = header;
        Footer = footer;
    }

    public static PageTemplate Create(string indexPath)
    {
        if (!File.Exists(indexPath))
            throw new Exception($"{Path.GetFullPath(indexPath)} does not exist");

        string? header = null;
        string? footer = null;

        var sb = new StringBuilder();
        foreach (var line in File.ReadAllLines(indexPath))
        {
            if (header == null)
            {
                if (line.Contains("<!--PAGE-->"))
                {
                    header = sb.ToString(); // capture up to start page marker
                    sb.Clear();
                }
                else sb.AppendLine(line);
            }
            else
            {
                if (sb.Length == 0)
                {
                    if (line.Contains("<!--/PAGE-->")) // discard up to end page marker
                    {
                        sb.AppendLine();
                        continue;
                    }
                }
                else sb.AppendLine(line);
            }
        }
        footer = sb.ToString();

        if (string.IsNullOrEmpty(header) || string.IsNullOrEmpty(footer))
            throw new Exception($"Parsing {indexPath} failed, missing <!--PAGE-->...<!--/PAGE--> markers");

        return new PageTemplate(header, footer);
    }

    public string Render(string body) => Header + body + Footer;
}
