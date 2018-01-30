using System;

namespace ServiceStack
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class AutoQueryViewerAttribute : AttributeBase
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string IconUrl { get; set; }
        public string BrandUrl { get; set; }
        public string BrandImageUrl { get; set; }
        public string TextColor { get; set; }
        public string LinkColor { get; set; }
        public string BackgroundColor { get; set; }
        public string BackgroundImageUrl { get; set; }
        public string DefaultSearchField { get; set; }
        public string DefaultSearchType { get; set; }
        public string DefaultSearchText { get; set; }
        public string DefaultFields { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class AutoQueryViewerFieldAttribute : AttributeBase
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public bool HideInSummary { get; set; }
        public string ValueFormat { get; set; }
        public string LayoutHint { get; set; }
    }
}