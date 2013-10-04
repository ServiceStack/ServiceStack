
using System;

namespace ServiceStack.DataAnnotations
{

    /// <summary>
    /// IgnoreAttribute
    /// Use to indicate that a property is not a field  in the table
    /// properties with this attribute are ignored when building sql sentences
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttribute : AttributeBase
    {
    }
}