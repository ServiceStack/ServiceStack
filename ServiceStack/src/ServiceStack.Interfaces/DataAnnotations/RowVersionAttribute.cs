using System;

namespace ServiceStack.DataAnnotations
{
    /// <summary>
    /// Used to indicate that property is a row version incremented automatically by the database
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class RowVersionAttribute : AttributeBase
    {
    }
}
