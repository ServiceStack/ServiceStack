using System;

namespace ServiceStack.DataAnnotations
{
    /// <summary>
    /// Range Key Attribute used to specify which property is the RangeKey, e.g. in DynamoDb.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class RangeKeyAttribute : AttributeBase
    {
    }
}