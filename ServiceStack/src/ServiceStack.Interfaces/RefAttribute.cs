using System;

namespace ServiceStack;

/// <summary>
/// Define UI References to external Data Models
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class RefAttribute : AttributeBase
{
    public string Model { get; set; }
    public string RefId { get; set; }
    public string RefLabel { get; set; }
    public string SelfId { get; set; }
}