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

        public TagAttribute(string name = null, ApplyTo applyTo = ApplyTo.All)
        {
            Name = name;
            ApplyTo = applyTo;
        }
    }
}
