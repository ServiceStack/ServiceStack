using System;

namespace ServiceStack
{
    /// <summary>
    /// Specify a VirtualPath or Layout for a Code Page
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
    public class PageAttribute : AttributeBase
    {
        public string VirtualPath { get; set; }
        public string Layout { get; set; }
        
        public PageAttribute(string virtualPath, string layout=null)
        {
            VirtualPath = virtualPath;
            Layout = layout;
        }
    }
    
    /// <summary>
    /// Specify static page arguments
    /// </summary>
    public class PageArgAttribute : AttributeBase
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public PageArgAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}
