using System;

namespace ServiceStack.DataAnnotations
{
    /// <summary>
    /// Define which RDBMS Schema Data Model belongs to
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SchemaAttribute : AttributeBase
    {
        public SchemaAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }
    }
}