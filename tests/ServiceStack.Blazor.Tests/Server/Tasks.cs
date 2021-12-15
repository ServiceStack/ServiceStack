using ServiceStack;
using ServiceStack.IO;
using System;
using System.IO;
using System.Text;
using static System.Console;

// Reuse Server App to build tasks like pre-rendering content
public static class TaskRunner
{
    public static Dictionary<string, ITask> Tasks = new()
    {
        ["prerender:markdown"] = new PrerenderMarkdownTask(),
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
        public string Task { get; set; }
        public string IndexPath { get; set; }
        public List<string> Args { get; set; }

        public static ArgsParser Parse(string[] args)
        {
            var to = new ArgsParser
            {
                Args = new(),
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
        CommandOption[] Options { get; set; }
        void Execute(ArgsParser cmd);
    }

    public class PrerenderMarkdownTask : ITask
    {
        public string Usage => @"Usage: -task prerender:markdown <src-dir> <dest-dir>";
        public CommandOption[] Options { get; set; } = new CommandOption[] {
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
            FileSystemVirtualFiles.DeleteDirectoryRecursive(dstDir);
            FileSystemVirtualFiles.AssertDirectory(dstDir);

            foreach (var file in new DirectoryInfo(srcDir).GetFiles("*.md", SearchOption.AllDirectories))
            {
                WriteLine($"Converting {file.FullName} ...");

                var name = file.Name.WithoutExtension();
                var docResult = MyApp.Client.MarkdownUtils.LoadDocumentAsync(name, async doc =>
                    File.ReadAllText(file.FullName)).GetAwaiter().GetResult();

                if (docResult.IsError)
                {
                    WriteLine($"Failed: {docResult.ErrorMessage}");
                    continue;
                }

                var mdBody = @$"<div class=""prose lg:prose-xl m-3"">
        <div class=""markdown-body"">
            {docResult.Response!.Preview!}
        </div>
    </div>";

                var prerenderedPage = IndexTemplate.Render(cmd, mdBody);
                File.WriteAllText(Path.Combine(dstDir, $"{name}.html"), prerenderedPage);
            }
        }
    }

    public class IndexTemplate
    {
        public string Contents { get; set; }
        public string Header { get; set; }
        public string Footer { get; set; }

        static IndexTemplate Instance = new();
        public static string Render(ArgsParser cmd, string body)
        {
            if (Instance.Contents == null)
            {
                if (!File.Exists(cmd.IndexPath))
                    throw new Exception($"{Path.GetFullPath(cmd.IndexPath)} does not exist");

                var sb = new StringBuilder();
                foreach (var line in File.ReadAllLines(cmd.IndexPath))
                {
                    if (Instance.Header == null)
                    {
                        sb.AppendLine(line);
                        if (line.Contains("id=\"app\""))
                        {
                            Instance.Header = sb.ToString(); // capture past <div id="app">
                            sb.Clear();
                        }
                    }
                    else
                    {
                        if (sb.Length == 0)
                        {
                            if (line.Contains("id=\"blazor-error-ui\"")) // discard up to <div id="blazor-error-ui">
                            {
                                sb.AppendLine("    </div>");
                                sb.AppendLine(line);
                                continue;
                            }
                        }
                        else
                        {
                            sb.AppendLine(line);
                        }
                    }
                }
                Instance.Footer = sb.ToString();

                if (Instance.Header == null)
                    throw new Exception("Parsing index.html failed, couldn't find <div id=\"app\"> marker");
            }
            return Instance.Header + body + Instance.Footer;
        }
    }
}
