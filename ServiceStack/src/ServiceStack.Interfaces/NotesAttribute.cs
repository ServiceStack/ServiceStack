using System;

namespace ServiceStack;

/// <summary>
/// Document a longer form description about a Type
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class NotesAttribute(string notes) : AttributeBase
{
    /// <summary>
    /// Get or sets a Label
    /// </summary>
    public string Notes { get; set; } = notes;
}