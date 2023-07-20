using System;

namespace ServiceStack
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class NamedConnectionAttribute : AttributeBase
    {
        public string Name { get; set; }

        public NamedConnectionAttribute(string name)
        {
            Name = name;
        }
    }
}