#nullable enable
using System;

namespace ServiceStack;

/// <summary>Selects a named ASP.NET Core rate-limiting policy for a request DTO.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RateLimitingAttribute : Attribute
{
    public string PolicyName { get; }
    public RateLimitingAttribute(string policyName) => PolicyName = policyName;
}
