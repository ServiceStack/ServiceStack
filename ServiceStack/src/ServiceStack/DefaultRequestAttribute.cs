using System;

namespace ServiceStack;

/// <summary>
/// Lets you Register new Services and the optional restPaths will be registered against 
/// this default Request Type
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class DefaultRequestAttribute : AttributeBase
{
    public Type RequestType { get; set; }

    public DefaultRequestAttribute(Type requestType) => RequestType = requestType;
    public string Verbs { get; set; }
}