
using System;

namespace ServiceStack.DataAnnotations
{

    /// <summary>
    /// BelongToAttribute
    /// Use to indicate that a join column belongs to another table.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class BelongToAttribute : AttributeBase
    {
        public Type BelongToTableType { get; set; }

        public BelongToAttribute(Type belongToTableType)
        {
            BelongToTableType = belongToTableType;
        }
    }
}