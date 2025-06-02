#nullable enable

using System;

namespace ServiceStack;

/// <summary>
/// Specify which File Upload location should be used to manage these file uploads
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class UploadToAttribute(string location) : AttributeBase
{
    public string Location { get; set; } = location;
}