using System;
using System.Collections.Generic;

namespace ServiceStack.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class UniqueConstraintAttribute : AttributeBase
    {
        public UniqueConstraintAttribute()
        {
            this.FieldNames = new List<string>();
        }

        public UniqueConstraintAttribute(params string[] fieldNames)
        {
            this.FieldNames = new List<string>(fieldNames);
        }

        public List<string> FieldNames { get; set; }

        public string Name { get; set; }
    }
}