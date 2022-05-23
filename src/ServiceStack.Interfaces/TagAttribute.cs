using System;

namespace ServiceStack
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class TagAttribute : AttributeBase
    {
        /// <summary>
        /// Get or sets tag name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Get or sets operation verbs for which the attribute be applied
        /// </summary>
        public ApplyTo ApplyTo { get; set; }

        public TagAttribute() : this(null) { }
        public TagAttribute(string name) : this(name, ApplyTo.All) {}
        public TagAttribute(string name, ApplyTo applyTo)
        {
            Name = name;
            ApplyTo = applyTo;
        }
    }

    public static class TagNames
    {
        public const string Auth = "auth";
    }
}
