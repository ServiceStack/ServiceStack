#nullable enable

using System;

namespace ServiceStack;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class TagAttribute : AttributeBase
{
    /// <summary>
    /// Get or sets tag name
    /// </summary>
    public string Name { get; set; }
    public TagAttribute() : this(null) { }
    public TagAttribute(string name) => Name = name;
}

public static class TagNames
{
    public const string Auth = "auth";
    public const string Admin = "admin";
    public const string Jobs = "jobs";
}