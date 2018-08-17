using System;

namespace ServiceStack.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
    public class IndexAttribute : AttributeBase
    {
        public IndexAttribute() { }

        public IndexAttribute(bool unique)
        {
            Unique = unique;
        }
        
        public string Name { get; set; }

        public bool Unique { get; set; }

        public bool Clustered { get; set; }

        public bool NonClustered { get; set; }
    }
}