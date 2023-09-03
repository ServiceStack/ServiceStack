using System;

namespace ServiceStack.DataAnnotations;

/// <summary>
/// Auto populate Primary Key Property with an RDBMS generated Auto Incrementing serial Integer
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class AutoIncrementAttribute : AttributeBase
{
}