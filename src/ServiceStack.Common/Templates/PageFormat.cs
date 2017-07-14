using System;
using System.IO;
using System.Threading.Tasks;
using ServiceStack.Text;

namespace ServiceStack.Templates
{
    public class PageFormat
    {
        public string ArgsPrefix { get; set; } = "---";

        public string ArgsSuffix { get; set; } = "---";
        
        public string Extension { get; set; }

        public string ContentType { get; set; } = MimeTypes.PlainText;

        public Func<object, string> EncodeValue { get; set; }

        public Func<TemplatePage, TemplatePage> ResolveLayout { get; set; }
        
        public Func<PageResult, Exception, object> OnExpressionException { get; set; }

        public PageFormat()
        {
            EncodeValue = DefaultEncodeValue;
            ResolveLayout = DefaultResolveLayout;
            OnExpressionException = DefaultExpressionException;
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
            page.Args.TryGetValue(TemplatePages.Layout, out object layout);
            return page.Context.Pages.ResolveLayoutPage(page, layout as string);
        }

        public virtual object DefaultExpressionException(PageResult result, Exception ex)
        {
            if (result.Page.Context.RenderExpressionExceptions)
                return $"{ex.GetType().Name}: ${ex.Message}";
            
            // Evaluate Null References in Binding Expressions to null
            if (ex is NullReferenceException || ex is ArgumentNullException)
                return JsNull.Value;

            return null;
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
            OnExpressionException = HtmlExpressionException;
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
        
        public virtual object HtmlExpressionException(PageResult result, Exception ex)
        {
            if (result.Page.Context.RenderExpressionExceptions)
                return ("<div style='color:red'><span>" + StringUtils.HtmlEncode(ex.GetType().Name + ": " + ex.Message) + "</span></div>").ToRawString();
            
            // Evaluate Null References in Binding Expressions to null
            if (ex is NullReferenceException || ex is ArgumentNullException)
                return JsNull.Value;

            return null;
        }

        public static async Task<Stream> HtmlEncodeTransformer(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var contents = await reader.ReadToEndAsync();
                var htmlEncoded = StringUtils.HtmlEncode(contents);
                return MemoryStreamFactory.GetStream(htmlEncoded.ToUtf8Bytes());
            }
        }
    }

}