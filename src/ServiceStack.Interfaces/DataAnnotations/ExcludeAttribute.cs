using System;

namespace ServiceStack.DataAnnotations
{
    /// <summary>
    /// Mark types that are to be excluded from specified features
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ExcludeAttribute : AttributeBase
    {
        public Feature Feature { get; set; }

        public ExcludeAttribute(Feature feature)
        {
            Feature = feature;
        }
    }
}