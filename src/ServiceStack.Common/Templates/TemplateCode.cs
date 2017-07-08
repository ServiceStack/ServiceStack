using System.Collections.Generic;

namespace ServiceStack.Templates
{
    public abstract class TemplateCode
    {
        public string VirtualPath { get; set; }
        public string Layout { get; set; }
        public object Model { get; set; }
        public Dictionary<string, object> Args { get; set; } = new Dictionary<string, object>();
        public ITemplatePages TemplatePages { get; set; }

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