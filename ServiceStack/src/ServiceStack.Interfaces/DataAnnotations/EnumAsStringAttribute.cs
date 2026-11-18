using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Store enum names in OrmLite columns independently of serialization settings.
/// </summary>
[AttributeUsage(AttributeTargets.Enum)]
public class EnumAsStringAttribute : AttributeBase
{
}
