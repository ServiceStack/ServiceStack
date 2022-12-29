using System;

namespace ServiceStack.DataAnnotations
{
    /// <summary>
    /// Save Enum value as single char in RDBMS column
    /// </summary>
    [AttributeUsage(AttributeTargets.Enum)]
    public class EnumAsCharAttribute : AttributeBase
    {
    }
}
