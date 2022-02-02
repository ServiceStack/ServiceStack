using System;

namespace ServiceStack.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    public class IdAttribute : AttributeBase
    {
        public int Id { get; }
        public IdAttribute(int id) => Id = id;
    }
}