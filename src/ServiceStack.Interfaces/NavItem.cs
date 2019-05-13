using System.Collections.Generic;

namespace ServiceStack
{
    /// <summary>
    /// NavItem in ViewUtils.NavItems and ViewUtils.NavItemsMap
    /// </summary>
    public class NavItem : IMeta
    {
        /// <summary>
        /// Link Label
        /// </summary>
        public string Label { get; set; }
        
        /// <summary>
        /// Link href
        /// </summary>
        public string Href { get; set; }
        
        /// <summary>
        /// Whether Active class should only be added when paths are exact match
        /// otherwise checks if ActivePath starts with Path
        /// </summary>
        public bool? Exact { get; set; }

        /// <summary>
        /// Emit id="{Id}"
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Override class="{Class}"
        /// </summary>
        public string ClassName { get; set; }
        /// <summary>
        /// HTML for Icon (if any)
        /// </summary>
        public string IconHtml { get; set; } 
        
        /// <summary>
        /// Only show if NavOptions.Attributes.Contains(Show) 
        /// </summary>
        public string Show { get; set; }
        
        /// <summary>
        /// Do not show if NavOptions.Attributes.Contains(Hide) 
        /// </summary>
        public string Hide { get; set; }
     
        /// <summary>
        /// Sub Menu Child NavItems
        /// </summary>
        public List<NavItem> Children { get; set; }
        
        public Dictionary<string, string> Meta { get; set; }
    }

}