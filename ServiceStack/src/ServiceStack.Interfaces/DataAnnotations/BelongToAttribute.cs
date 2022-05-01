
using System;

namespace ServiceStack.DataAnnotations
{

    /// <summary>
    /// Populate property from ambiguous column name in the specified joined table type
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