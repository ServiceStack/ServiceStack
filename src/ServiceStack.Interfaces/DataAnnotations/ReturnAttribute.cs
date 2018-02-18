
using System;

namespace ServiceStack.DataAnnotations
{

    /// <summary>
    /// ReturnAttribute
    /// Use to indicate that a property should be included in the 
    /// returning/output clause of INSERT and UPDATE sql sentences
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ReturnAttribute : AttributeBase {}

    /// <summary>
    /// Return this property in INSERT statements
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ReturnOnInsertAttribute : AttributeBase { }

    /// <summary>
    /// Return this property in UPDATE statements
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ReturnOnUpdateAttribute : AttributeBase { }
}