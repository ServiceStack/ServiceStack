using System;

namespace ServiceStack.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class ReferencesAttribute : AttributeBase
    {
        public Type Type { get; set; }

        public ReferencesAttribute(Type type)
        {
            this.Type = type;
        }
    }
}