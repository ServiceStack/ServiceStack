
using System;

namespace ServiceStack.DataAnnotations
{

    /// <summary>
    /// ReturnAttribute
    /// Use to indicate that a property should be included in the 
    /// returning/output clause of INSERT sql sentences
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ReturnOnInsertAttribute : AttributeBase { }
}