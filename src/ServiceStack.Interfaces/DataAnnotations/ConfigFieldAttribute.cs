using System;

namespace ServiceStack.DataAnnotations;

[AttributeUsage(AttributeTargets.Property)]
public class ConfigFieldAttribute : AttributeBase
{
    public int Order { get; set; }
}