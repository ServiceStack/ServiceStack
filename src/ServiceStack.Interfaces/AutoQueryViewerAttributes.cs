using System;

namespace ServiceStack
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class AutoQueryViewerAttribute : AttributeBase
    {
        public string Title { get; set; }
        public string Description { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class AutoQueryViewerFieldAttribute : AttributeBase
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public bool HideInSummary { get; set; }
        public string ValueFormat { get; set; }
        public int? Flex { get; set; }
        public string LayoutHint { get; set; }
    }
}