using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.IO;
using ServiceStack.Text;

#if NETSTANDARD1_3
using Microsoft.Extensions.Primitives;
#endif

namespace ServiceStack.Templates
{
    public class TemplatePagesContext
    {
        public List<PageFormat> PageFormats { get; set; } = new List<PageFormat>();
        
        public string IndexPage { get; set; } = "index";

        public string DefaultLayoutPage { get; set; } = "_layout";
        
        public string LayoutVarName { get; set; } = "layout";

        public bool CheckModifiedPages { get; set; } = false;

        public ITemplatePages TemplatePages { get; set; }

        public IVirtualPathProvider VirtualFileSources { get; set; }
        
        public bool DebugMode { get; set; }

        public PageFormat GetFormat(string extension) => PageFormats.FirstOrDefault(x => x.Extension == extension);

        public TemplatePagesContext()
        {
            TemplatePages = new TemplatePages(this);
            PageFormats.Add(new HtmlPageFormat());
        }
    }

    public class PageFormat
    {
        public string ArgsPrefix { get; set; } = "---";

        public string ArgsSuffix { get; set; } = "---";
        
        public string Extension { get; set; }

        public string ContentType { get; set; } = MimeTypes.PlainText;

        public Func<object, string> EncodeValue { get; set; }

        public Func<TemplatePage, TemplatePage> ResolveLayout { get; set; }

        public PageFormat()
        {
            EncodeValue = DefaultEncodeValue;
            ResolveLayout = DefaultResolveLayout;
        }

        public string DefaultEncodeValue(object value)
        {
            if (value is IRawString rawString)
                return rawString.ToRawString();
            
            var str = value.ToString();
            if (str == string.Empty)
                return string.Empty;

            return str;
        }

        public TemplatePage DefaultResolveLayout(TemplatePage page)
        {
            page.PageVars.TryGetValue(TemplatePages.Layout, out string layout);
            return page.Context.TemplatePages.ResolveLayoutPage(page, layout);
        }
    }

    public class HtmlPageFormat : PageFormat
    {
        public HtmlPageFormat()
        {
            ArgsPrefix = "<!--";
            ArgsSuffix = "-->";
            Extension = "html";
            ContentType = MimeTypes.Html;
            EncodeValue = HtmlEncodeValue;
            ResolveLayout = HtmlResolveLayout;
        }
        
        public static string HtmlEncodeValue(object value)
        {
            if (value == null)
                return string.Empty;
            
            if (value is IHtmlString htmlString)
                return htmlString.ToHtmlString();

            if (value is IRawString rawString)
                return rawString.ToRawString();
            
            var str = value.ToString();
            if (str == string.Empty)
                return string.Empty;

            return StringUtils.HtmlEncode(str);
        }

        public TemplatePage HtmlResolveLayout(TemplatePage page)
        {
            var isCompletePage = page.BodyContents.StartsWith("<!DOCTYPE HTML>") || page.BodyContents.StartsWithIgnoreCase("<html");
            if (isCompletePage)
                return null;

            return base.DefaultResolveLayout(page);
        }
    }
}
