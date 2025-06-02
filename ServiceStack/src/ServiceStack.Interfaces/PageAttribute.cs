#nullable enable

using System;

namespace ServiceStack;

/// <summary>
/// Specify a VirtualPath or Layout for a Code Page
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public class PageAttribute(string virtualPath, string? layout = null) : AttributeBase
{
    public string VirtualPath { get; set; } = virtualPath;
    public string? Layout { get; set; } = layout;
}
    
/// <summary>
/// Specify static page arguments
/// </summary>
public class PageArgAttribute(string name, string? value) : AttributeBase
{
    public string Name { get; set; } = name;
    public string? Value { get; set; } = value;
}