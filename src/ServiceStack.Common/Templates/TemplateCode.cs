using System.Collections.Generic;

namespace ServiceStack.Templates
{
    public abstract class TemplateCode
    {
        public string VirtualPath { get; set; }
        public string Layout { get; set; }
        public object Model { get; set; }
        public Dictionary<string, object> Args { get; } = new Dictionary<string, object>();
        public ITemplatePages Pages { get; set; }

        protected TemplateCode(string virtualPath = null, string layout = null)
        {
            VirtualPath = virtualPath;
            Layout = layout;
        }

        public TemplateCode Init()
        {
            return this;
        }
    }
}