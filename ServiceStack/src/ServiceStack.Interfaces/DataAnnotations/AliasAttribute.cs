using System;

namespace ServiceStack.DataAnnotations
{
    /// <summary>
    /// Map C# Type Name to a different RDBMS Table name or a Property Name to a different RDBMS Column name
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method)]
    public class AliasAttribute : AttributeBase
    {
        public string Name { get; set; }

        public AliasAttribute(string name)
        {
            this.Name = name;
        }
    }
}