#nullable enable

using System;
using ServiceStack.DataAnnotations;

namespace ServiceStack.Jobs;

/// <summary>
/// Data Model to capture a reoccurring task
/// </summary>
public class ScheduledTask
{
    [AutoIncrement]
    public long Id { get; set; }
    [Index(Unique = true)]
    public string Name { get; set; }
    public TimeSpan? Interval { get; set; }
    public string? CronExpression { get; set; }
    public virtual string RequestType { get; set; }
    public virtual string? Command { get; set; }
    public virtual string Request { get; set; }
    public virtual string RequestBody { get; set; }
    public BackgroundJobOptions? Options { get; set; }
    public DateTime? LastRun { get; set; }
    public long? LastJobId { get; set; }
}

/// <summary>
/// Represents the scheduled task to run at intervals or by Cron expression
/// </summary>
public class Schedule
{
    TimeSpan? _interval { get; }
    string? _cronExpression { get; }

    /// <summary>
    /// Create a schedule with an interval
    /// </summary>
    public Schedule(TimeSpan? interval) => _interval = interval;
    /// <summary>
    /// Run on a specific interval specified by a cron expression, see: https://en.wikipedia.org/wiki/Cron
    /// </summary>
    public Schedule(string? cronExpression) => _cronExpression = cronExpression;
    
    /// <summary>
    /// Create a schedule with an interval
    /// </summary>
    public static Schedule Interval(TimeSpan interval) => new(interval);
    /// <summary>
    /// Run on a specific interval specified by a cron expression, see: https://en.wikipedia.org/wiki/Cron
    /// </summary>
    public static Schedule Cron(string cronExpression) => new(cronExpression);
    /// <summary>
    /// Run once a minute at the beginning of the minute
    /// </summary>
    public static Schedule EveryMinute => new("* * * * *");
    /// <summary>
    /// Run once an hour at the beginning of the hour
    /// </summary>
    public static Schedule Hourly => new("0 * * * *");
    /// <summary>
    /// Run once a day at midnight
    /// </summary>
    public static Schedule Daily => new("0 0 * * *");
    /// <summary>
    /// Run once a week at midnight on Sunday morning
    /// </summary>
    public static Schedule Weekly => new("0 0 * * 0");
    /// <summary>
    /// Run once a month at midnight of the first day of the month
    /// </summary>
    public static Schedule Monthly => new("0 0 1 * *");
    /// <summary>
    /// Run once a year at midnight of 1 January
    /// </summary>
    public static Schedule Yearly => new("0 0 1 * *");

    public void Deconstruct(out TimeSpan? interval, out string? cronExpression)
    {
        interval = _interval;
        cronExpression = _cronExpression;
    }
}
