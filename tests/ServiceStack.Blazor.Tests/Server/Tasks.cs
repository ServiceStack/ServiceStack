using ServiceStack;
using ServiceStack.IO;
using System.Text;
using static System.Console;

// Reuse Server App to run post build tasks like pre-rendering SSG content like markdown pages
// Tasks are automatically run in GitHub Actions release.yml but can be run locally with:
// $ dotnet build /p:APP_TASKS=prerender:markdown
public static class TaskRunner
{
    public static Dictionary<string, ITask> Tasks = new()
    {
        ["prerender:markdown"] = new PrerenderMarkdownTask(),
        ["prerender:clean"] = new PrerenderClean(),
    };

    public static void Handle(string[] mainArgs)
    {
        var cmd = ArgsParser.Parse(mainArgs);

        if (cmd.Task != null)
        {
            if (!Tasks.TryGetValue(cmd.Task, out var task))
            {
                WriteLine($"Unknown task: {cmd.Task}");
                Environment.Exit(-1);
            }

            try
            {
                task.Execute(cmd);
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                if (cmd.Verbose)
                    WriteLine(ex.ToString());
                else
                    WriteLine(ex.Message);

                WriteLine();
                WriteLine(task.Usage);
                WriteLine();
                WriteLine("Options:");

                string fmt(CommandOption o) => (o.Flag != null ? $"{o.Flag}, " : "") + o.Switch + "  ";
                var allOptions = new List<CommandOption>(task.Options) {
                    new("-verbose", "Display verbose logging"),
                };
                var padSwitch = Math.Max(allOptions.Select(o => fmt(o).Length).Max() + 4, 12);
                allOptions.Each(o => WriteLine($"    {fmt(o)}".PadRight(padSwitch, ' ') + o.Description));

                Environment.Exit(-1);
            }
        }
    }

    public class ArgsParser
    {
        public static string DefaultIndexPath { get; set; } = "../MyApp.Client/wwwroot/index.html";

        public bool Verbose { get; set; }
        public string? Task { get; set; }
        public string? IndexPath { get; set; }
        public List<string> Args { get; set; } = new();

        public static ArgsParser Parse(string[] args)
        {
            var to = new ArgsParser
            {
                IndexPath = DefaultIndexPath,
            };

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i]) {
                    case "-task":
                        to.Task = args[++i];
                        break;
                    case "-index":
                        to.IndexPath = args[++i];
                        break;
                    case "-verbose":
                        to.Verbose = true;
                        break;
                    default:
                        to.Args.Add(args[i]);
                        break;
                };
            }

            return to;
        }
    }

    public record CommandOption(string Switch, string Description, string? Flag=null);

    public interface ITask
    {
        string Usage { get; }
        CommandOption[] Options { get; }
        void Execute(ArgsParser cmd);
    }

    /// <summary>
    /// Renders the Markdown Content using same Markding configuration used in Docs.razor inside pre-rendered index.html loading page
    /// </summary>
    public class PrerenderMarkdownTask : ITask
    {
        public string Usage => @"Usage: -task prerender:markdown <src-dir> <dest-dir>";
        public CommandOption[] Options => new CommandOption[] {
            new("-index <path>", "Path to index.html"),
        };
        public void Execute(ArgsParser cmd)
        {
            if (cmd.Args.Count < 2) throw new Exception("Too few arguments");

            string santizePath(string path) => Path.DirectorySeparatorChar == '\\'
                ? path.Replace('/', '\\')
                : path.Replace('\\', '/');

            var srcDir = santizePath(cmd.Args[0]);
            var dstDir = santizePath(cmd.Args[1]);

            if (!Directory.Exists(srcDir)) throw new Exception($"{Path.GetFullPath(srcDir)} does not exist");
            if (Directory.Exists(dstDir)) FileSystemVirtualFiles.DeleteDirectoryRecursive(dstDir);
            FileSystemVirtualFiles.AssertDirectory(dstDir);

            foreach (var file in new DirectoryInfo(srcDir).GetFiles("*.md", SearchOption.AllDirectories))
            {
                WriteLine($"Converting {file.FullName} ...");

                var name = file.Name.WithoutExtension();
                var docRender = MyApp.Client.MarkdownUtils.LoadDocumentAsync(name, doc =>
                    Task.FromResult(File.ReadAllText(file.FullName))).GetAwaiter().GetResult();

                if (docRender.Failed)
                {
                    WriteLine($"Failed: {docRender.ErrorMessage}");
                    continue;
                }

                var mdBody = @$"
<div class=""prose lg:prose-xl min-vh-100 m-3"">
    <div class=""markdown-body"">
        {docRender.Response!.Preview!}
    </div>
</div>
";

                var prerenderedPage = IndexTemplate.Render(cmd, mdBody);
                string htmlPath = Path.GetFullPath(Path.Combine(dstDir, $"{name}.html"));
                File.WriteAllText(htmlPath, prerenderedPage);
                WriteLine($"Written to {htmlPath}");

            }
        }
    }

    /// <summary>
    /// Simple cleanup of concatenated Blazor pages by removing @attributes and @code{} blocks
    /// </summary>
    public class PrerenderClean : ITask
    {
        public string Usage => @"Usage: -task prerender:clean <dir>";
        public CommandOption[] Options => Array.Empty<CommandOption>();

        public void Execute(ArgsParser cmd)
        {
            if (cmd.Args.Count < 1) throw new Exception("Too few arguments");

            var prerenderDir = cmd.Args[0];

            foreach (var file in new DirectoryInfo(prerenderDir).GetFiles("*.html", SearchOption.AllDirectories))
            {
                var sb = new StringBuilder();
                foreach (var line in File.ReadAllLines(file.FullName))
                {
                    if (line.StartsWith("@code"))
                        break;
                    if (line.StartsWith("@"))
                        continue;
                    sb.AppendLine(line);
                }
                sb.AppendLine("<!--prerendered-->"); // marker to identify it's a prendered page
                File.WriteAllText(file.FullName, sb.ToString());
            }

        }
    }

    /// <summary>
    /// Parses wwwroot/index.html and uses its layout to generate prerendered pages inside <!--PAGE--><!--/PAGE-->
    /// </summary>
    public class IndexTemplate
    {
        public string? Contents { get; set; }
        public string? Header { get; set; }
        public string? Footer { get; set; }

        static IndexTemplate Instance = new();
        public static string Render(ArgsParser cmd, string body)
        {
            if (Instance.Contents == null)
            {
                if (!File.Exists(cmd.IndexPath))
                    throw new Exception($"{Path.GetFullPath(cmd.IndexPath!)} does not exist");

                var sb = new StringBuilder();
                foreach (var line in File.ReadAllLines(cmd.IndexPath))
                {
                    if (Instance.Header == null)
                    {
                        if (line.Contains("<!--PAGE-->"))
                        {
                            Instance.Header = sb.ToString(); // capture up to start page marker
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
                Instance.Footer = sb.ToString();

                if (Instance.Header == null)
                    throw new Exception("Parsing index.html failed, missing <!--PAGE-->...<!--/PAGE--> markers");
            }
            return Instance.Header + body + Instance.Footer;
        }
    }
}
