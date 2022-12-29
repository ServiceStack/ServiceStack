
using System;

namespace ServiceStack.DataAnnotations
{

    /// <summary>
    /// Indicate the property should be included in the returning/output clause of INSERT SQL Statements
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ReturnOnInsertAttribute : AttributeBase { }
}