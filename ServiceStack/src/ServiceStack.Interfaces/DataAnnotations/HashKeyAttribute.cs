using System;

namespace ServiceStack.DataAnnotations
{
    /// <summary>
    /// Hash Key Attribute used to specify which property is the HashKey, e.g. in DynamoDb.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class HashKeyAttribute : AttributeBase
    {
    }
}