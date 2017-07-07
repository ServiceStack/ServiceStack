using System;
using ServiceStack.IO;
using ServiceStack.Text;

#if NETSTANDARD1_3
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public class TemplatePagesContext
    {
        public static int PreventDosMaxSize = 10000; 
        
        public string PageExtension { get; set; } = "html";
        
        public string IndexPage { get; set; } = "index";

        public string DefaultLayoutPage { get; set; } = "_layout";
        
        public string LayoutVarName { get; set; } = "layout";

        public bool CheckModifiedPages { get; set; } = false;

        public ITemplatePages TemplatePages { get; set; }

        public IVirtualPathProvider VirtualFileSources { get; set; }

        public Func<object, string> EncodeValue { get; set; } = TemplatePageUtils.HtmlEncodeValue;

        public Func<StringSegment, bool> IsCompletePage = page => 
            page.StartsWith("<!DOCTYPE HTML>") || page.StartsWithIgnoreCase("<html");
        
        public bool DebugMode { get; set; }
        
        public TemplatePagesContext()
        {
            TemplatePages = new TemplatePages(this);
        }
    }
}