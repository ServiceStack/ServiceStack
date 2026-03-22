#nullable enable
#if NET8_0_OR_GREATER
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.Cronos;
using ServiceStack.Jobs;

namespace ServiceStack;

public partial class DbJobs
{
    private ConcurrentDictionary<string, ScheduledTask> namedScheduledTasks = new();
    private ConcurrentDictionary<string, CronExpression> cronExpressions = new();

    public ICollection<ScheduledTask> ScheduledTasks => namedScheduledTasks.Values;
    public ICollection<CronExpression> CronExpressions => cronExpressions.Values;

    /// <summary>
    /// On Startup load all scheduled tasks into memory
    /// </summary>
    void LoadScheduledTasks()
    {
        using var db = feature.OpenDb();
        var tasks = db.Select<ScheduledTask>();
        foreach (var task in tasks)
        {
            namedScheduledTasks[task.Name] = task;
            if (task.CronExpression != null && !cronExpressions.ContainsKey(task.CronExpression))
            {
                cronExpressions[task.CronExpression] = CronExpression.Parse(task.CronExpression);
            }
        }
    }
    
    private void CreateOrUpdate(ScheduledTask task)
    {
        using var db = feature.OpenDb();
        var updated = db.UpdateOnly(() => new ScheduledTask
        {
            Interval = task.Interval,
            CronExpression = task.CronExpression,
            RequestType = task.RequestType,
            Command = task.Command,
            Request = task.Request,
            RequestBody = task.RequestBody,
            Options = task.Options,
        }, where:x => x.Name == task.Name);
        if (updated == 0)
            task.Id = db.Insert(task, selectIdentity:true);
        namedScheduledTasks[task.Name] = task;
    }

    public void RecurringCommand(string taskName, Schedule schedule, string commandName, object arg, BackgroundJobOptions? options = null)
    {
        var task = namedScheduledTasks.GetOrAdd(taskName, _ => new ScheduledTask {
            Name = taskName
        });
        var (interval, cronExpression) = schedule;
        task.Interval = interval;
        task.CronExpression = cronExpression;
        task.RequestType = CommandResult.Command;
        task.Command = commandName;
        task.Request = arg.GetType().Name;
        task.RequestBody = ClientConfig.ToJson(arg);
        task.Options = options;
        
        CreateRequestForCommand(task.Command!, task.Request, task.RequestBody); // Ensure Request DTO can be recreated
        
        CreateOrUpdate(task);
    }

    public void RecurringApi(string taskName, Schedule schedule, object requestDto, BackgroundJobOptions? options = null)
    {
        var task = namedScheduledTasks.GetOrAdd(taskName, _ => new ScheduledTask {
            Name = taskName
        });
        var (interval, cronExpression) = schedule;
        task.Interval = interval;
        task.CronExpression = cronExpression;
        task.RequestType = CommandResult.Api;
        task.Command = null;
        task.Request = requestDto.GetType().Name;
        task.RequestBody = ClientConfig.ToJson(requestDto);
        task.Options = options;

        if (feature.AppHost.Metadata.GetServiceTypeByRequest(requestDto.GetType()) == null)
            throw new NotSupportedException("Service not found for request type: " + requestDto.GetType().Name);
        CreateRequestForApi(task.Request, task.RequestBody); // Ensure Request DTO can be recreated
        
        CreateOrUpdate(task);
    }
    
    public void DeleteRecurringTask(string taskName)
    {
        namedScheduledTasks.Remove(taskName, out _);
        using var db = OpenDb();
        db.Delete<ScheduledTask>(x => x.Name == taskName);
    }

    void ExecuteDueScheduledTasks()
    {
        var scheduledTasks = namedScheduledTasks.Values.ToList();
        foreach (var task in scheduledTasks)
        {
            var lastRun = task.LastRun ?? DateTime.UtcNow.AddYears(-1);
            if (task.Interval != null)
            {
                if (task.LastRun == null || DateTime.UtcNow - lastRun >= task.Interval)
                {
                    EnqueueNewJobForScheduledTask(task);
                }
            }
            else if (task.CronExpression != null)
            {
                var cron = cronExpressions.GetOrAdd(task.CronExpression, CronExpression.Parse);
                if (cron.GetNextOccurrence(lastRun) <= DateTime.UtcNow)
                {
                    EnqueueNewJobForScheduledTask(task);
                }
            }
        }
    }

    private void EnqueueNewJobForScheduledTask(ScheduledTask task)
    {
        var expectedLastRun = task.LastRun;
        var now = DateTime.UtcNow;

        // Optimistically claim this scheduled task tick before enqueuing.
        // With multiple instances polling on the same interval, two instances can both observe
        // the same stale LastRun and both decide the task is due. By making the DB update the
        // claim step (conditional on LastRun still matching what we observed), only the first
        // instance to reach the DB wins — the loser gets 0 rows updated and returns without
        // enqueuing, eliminating duplicate jobs across instances.
        using var db = feature.OpenDb();
        var claimed = db.UpdateOnly(() => new ScheduledTask {
            LastRun = now,
        }, where: x => x.Id == task.Id && (x.LastRun == null || x.LastRun == expectedLastRun));

        if (claimed == 0)
            return; // Another instance already claimed this scheduled task tick

        task.LastRun = now;

        BackgroundJobRef? jobRef = null;
        if (task.RequestType == CommandResult.Command)
        {
            var request = CreateRequestForCommand(task.Command!, task.Request, task.RequestBody);
            if (task.Options?.RunCommand == true)
            {
                RunCommand(task.Command!, request, task.Options);
            }
            else
            {
                jobRef = EnqueueCommand(task.Command!, request, task.Options);
                task.LastJobId = jobRef.Id;
            }
        }
        else if (task.RequestType == CommandResult.Api)
        {
            var request = CreateRequestForApi(task.Request, task.RequestBody);
            jobRef = EnqueueApi(request, task.Options);
            task.LastJobId = jobRef.Id;
        }
        else throw new NotSupportedException("Unsupported RequestType: " + task.RequestType);

        if (task.LastJobId != null)
        {
            db.UpdateOnly(() => new ScheduledTask {
                LastJobId = task.LastJobId,
            }, where: x => x.Id == task.Id);
        }
    }

    public void ClearScheduledTasks()
    {
        namedScheduledTasks.Clear();
        cronExpressions.Clear();
    }
}
#endif