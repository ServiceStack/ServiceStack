#nullable enable
using System;

namespace ServiceStack;

/// <summary>
/// Worker Thread names
/// </summary>
public static class Workers
{
    /// <summary>
    /// Worker name for OrmLite default DB Connection
    /// </summary>
    public const string AppDb = "app.db";
    public const string JobsDb = "jobs.db";
    public const string RequestsDb = "requests.db";
}

/// <summary>
/// Execute AutoQuery Create/Update/Delete Request DTO in a background thread
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class WorkerAttribute : AttributeBase
{
    public string Name { get; set; }
    public WorkerAttribute(string name) => Name = name;
}
