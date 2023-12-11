using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Uniquely identify C# Types and properties with a unique integer in gRPC Services
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public class IdAttribute : AttributeBase
{
    public int Id { get; }
    public IdAttribute(int id) => Id = id;
}