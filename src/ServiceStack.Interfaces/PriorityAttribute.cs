using System;

namespace ServiceStack
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    public class PriorityAttribute : AttributeBase
    {
        public int Value { get; set; }
        public PriorityAttribute(int value) => Value = value;
    }
}