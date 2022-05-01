using System;

namespace ServiceStack;

/// <summary>
/// Document a longer form description about a Type
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class NotesAttribute : AttributeBase
{
    /// <summary>
    /// Get or sets a Label
    /// </summary>
    public string Notes { get; set; }
    public NotesAttribute(string notes) => Notes = notes;
}