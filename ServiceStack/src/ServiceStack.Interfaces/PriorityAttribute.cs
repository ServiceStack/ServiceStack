using System;

namespace ServiceStack
{
    /// <summary>
    /// Specify the order in which legacy Modular Startup classes are run
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    public class PriorityAttribute : AttributeBase
    {
        public int Value { get; set; }
        public PriorityAttribute(int value) => Value = value;
    }
}