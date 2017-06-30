
using System;

namespace ServiceStack.DataAnnotations
{

    /// <summary>
    /// IgnoreAttribute
    /// Use to indicate that a property is not a field  in the table
    /// properties with this attribute are ignored when building sql sentences
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreAttribute : AttributeBase {}

    /// <summary>
    /// Ignore this property in SELECT statements
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreOnSelectAttribute : AttributeBase { }

    /// <summary>
    /// Ignore this property in UPDATE statements
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreOnUpdateAttribute : AttributeBase { }

    /// <summary>
    /// Ignore this property in INSERT statements
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IgnoreOnInsertAttribute : AttributeBase { }
}