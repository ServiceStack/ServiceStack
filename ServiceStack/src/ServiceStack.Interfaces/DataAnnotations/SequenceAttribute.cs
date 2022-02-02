using System;

namespace ServiceStack.DataAnnotations
{
    /// <summary>
    /// Sequence attribute.
    /// Use in FirebirdSql. indicates name of generator for columns of type AutoIncrement
    // </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SequenceAttribute : AttributeBase
    {
        public string Name { get; set; }

        public SequenceAttribute(string name)
        {
            this.Name = name;
        }
    }
}