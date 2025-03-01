#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Logging;
using ServiceStack.Text;

namespace ServiceStack;

public interface IAppTask
{
    public string? Log { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedDate { get; set; }
    public Exception? Error { get; set; }
}

public class AppTaskResult
{
    public AppTaskResult(List<IAppTask> tasksRun)
    {
        TasksRun = tasksRun;
        TypesCompleted = tasksRun.Where(x => x.Error == null).Map(x => x.GetType());
    }

    public string GetLogs()
    {
        var sb = StringBuilderCache.Allocate();
        foreach (var instance in TasksRun)
        {
            var migrationType = instance.GetType();
            var descFmt = AppTasks.GetDescFmt(migrationType);
            sb.AppendLine($"# {migrationType.Name}{descFmt}");
            sb.AppendLine(instance.Log);
            sb.AppendLine();
        }
        return StringBuilderCache.ReturnAndFree(sb);
    }

    public Exception? Error { get; set; }
    public List<Type> TypesCompleted { get; }
    public List<IAppTask> TasksRun { get; }
    public bool Succeeded => Error == null && TasksRun.All(x => x.Error == null);
}

public class AppTasks
{
    public static AppTasks Instance { get; set; } = new();
    public ILog Log { get; set; } = new ConsoleLogger(typeof(AppTasks));
    public Dictionary<string, Action<string[]>> Tasks { get; } = new();

    /// <summary>
    /// Register Task to run in APP_TASKS=task1;task2
    /// </summary>
    public static void Register(string taskName, Action<string[]> appTask)
    {
        Instance.Tasks[taskName] = appTask;
    }

    public static string? GetAppTaskCommands() => GetAppTaskCommands(Environment.GetCommandLineArgs());

    public static string? GetAppTaskCommands(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.IndexOf('=') == -1)
                continue;
            var key = arg.LeftPart('=').TrimPrefixes("/", "--");
            if (key == nameof(AppTasks))
                return arg.RightPart('=');
        }
        return null;
    }

    public static bool IsRunAsAppTask() => GetAppTaskCommands() != null;

    public static int? RanAsTask()
    {
        var appTasksStr = GetAppTaskCommands();
        if (appTasksStr != null)
        {
            var tasks = Instance.Tasks;
            if (tasks.Count > 0)
            {
                var appTasks = appTasksStr.Split(';');
                for (var i = 0; i < appTasks.Length; i++)
                {
                    var appTaskWithArgs = appTasks[i];
                    var appTask = appTaskWithArgs.LeftPart(':');
                    var args = appTaskWithArgs.IndexOf(':') >= 0
                        ? appTaskWithArgs.RightPart(':').Split(',')
                        : [];
                    
                    if (!tasks.TryGetValue(appTask, out var taskFn))
                    {
                        Instance.Log.Warn($"Unknown AppTask '{appTask}' was not registered with this App, ignoring...");
                        continue;
                    }

                    var exitCode = 0;
                    try
                    {
                        Instance.Log.Info($"Running AppTask '{appTask}'...");
                        taskFn(args);
                    }
                    catch (Exception e)
                    {
                        exitCode = i + 1; // return 1-based index of AppTask that failed
                        Instance.Log.Error($"Failed to run AppTask '{appTask}'", e);
                    }
                    return exitCode;
                }
            }
            else
            {
                Instance.Log.Info("No AppTasks to run, exiting...");
            }
            return 0;
        }
        return null;
    }

    public static void Run(Action? onExit=null)
    {
        var exitCode = RanAsTask();
        if (exitCode != null)
        {
            onExit?.Invoke();
            Environment.Exit(exitCode.Value);
            // Trying to Stop Application before app.Run() throws Unhandled exception. System.OperationCanceledException
            // var appLifetime = ApplicationServices.Resolve<IHostApplicationLifetime>();
            // Environment.ExitCode = exitCode;
            // appLifetime.StopApplication();
        }
    }

    public static string GetDescFmt(Type nextRun)
    {
        var desc = GetDesc(nextRun);
        return desc != null ? " '" + desc + "'" : "";
    }

    public static string? GetDesc(Type nextRun)
    {
        var desc = nextRun.GetDescription() ?? nextRun.FirstAttribute<NotesAttribute>()?.Notes;
        return desc;
    }
    
}