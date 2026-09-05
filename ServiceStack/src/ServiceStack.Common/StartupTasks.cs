#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Logging;

namespace ServiceStack;

/// <summary>
/// Tasks to run once after the application has started. ServiceStack automatically runs
/// registered startup tasks in DebugMode.
/// </summary>
public class StartupTasks
{
    public static StartupTasks Instance { get; set; } = new();

    public ILog Log { get; set; } = new ConsoleLogger(typeof(StartupTasks));

    public Dictionary<string, Action> Tasks { get; } = new();

    public static void Register(string taskName, Action startupTask)
    {
        if (string.IsNullOrWhiteSpace(taskName))
            throw new ArgumentNullException(nameof(taskName));
        if (startupTask == null)
            throw new ArgumentNullException(nameof(startupTask));

        var instance = Instance ??= new();
        lock (instance.Tasks)
        {
            instance.Tasks[taskName] = startupTask;
        }
    }

    public static void Run()
    {
        var instance = Instance;
        if (instance == null) return;

        KeyValuePair<string, Action>[] entries;
        lock (instance.Tasks)
        {
            entries = instance.Tasks.ToArray();
        }

        var log = instance.Log ?? LogManager.GetLogger(typeof(StartupTasks));
        foreach (var entry in entries)
        {
            try
            {
                log.Info($"Running StartupTask '{entry.Key}'...");
                entry.Value();
            }
            catch (Exception e)
            {
                // Startup tasks are development conveniences and shouldn't prevent the host starting.
                log.Error($"StartupTask '{entry.Key}' failed", e);
            }
        }
    }
}
