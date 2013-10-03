using System;

namespace ServiceStack.DataAnnotations
{
    /// <summary>
    /// Decorate any type or property with adhoc info
    /// </summary>
    public class MetaAttribute : AttributeBase
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public MetaAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}