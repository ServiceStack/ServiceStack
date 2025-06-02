using System.Collections.Concurrent;
using ServiceStack.OrmLite;
using Cronos;

namespace ServiceStack.Jobs;

public partial class BackgroundJobs
{
    private ConcurrentDictionary<string, ScheduledTask> namedScheduledTasks = new();
    private ConcurrentDictionary<string, CronExpression> cronExpressions = new();

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
        lock (dbTransactions)
        {
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
        }
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
        lock (dbTransactions)
        {
            db.Delete<ScheduledTask>(x => x.Name == taskName);
        }
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
        BackgroundJobRef? jobRef;
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

        task.LastRun = DateTime.UtcNow;
        using var db = feature.OpenDb();
        lock (dbTransactions)
        {
            db.UpdateOnly(() => new ScheduledTask
            {
                LastRun = task.LastRun,
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

