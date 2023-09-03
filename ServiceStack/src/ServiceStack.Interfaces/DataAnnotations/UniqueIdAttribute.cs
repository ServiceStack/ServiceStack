using System;

namespace ServiceStack.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
public class UniqueIdAttribute : AttributeBase
{
    public int Id { get; }
    public UniqueIdAttribute(int id) => Id = id;
}