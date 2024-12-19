using System;

namespace ServiceStack.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public class UniqueIdAttribute(int id) : AttributeBase
{
    public int Id { get; } = id;
}