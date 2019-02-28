using System;
using System.IO;
using System.Threading.Tasks;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.Script
{
    public class PageFormat
    {
        public string ArgsPrefix { get; set; } = "---";

        public string ArgsSuffix { get; set; } = "---";
        
        public string Extension { get; set; }

        public string ContentType { get; set; } = MimeTypes.PlainText;

        public Func<object, string> EncodeValue { get; set; }

        public Func<SharpPage, SharpPage> ResolveLayout { get; set; }
        
        public Func<PageResult, Exception, object> OnExpressionException { get; set; }
        
        public Func<PageResult, IRequest, Exception, Task> OnViewException { get; set; }

        public PageFormat()
        {
            EncodeValue = DefaultEncodeValue;
            ResolveLayout = DefaultResolveLayout;
            OnExpressionException = DefaultExpressionException;
            OnViewException = DefaultViewException;
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

        public SharpPage DefaultResolveLayout(SharpPage page)
        {
            page.Args.TryGetValue(SharpPages.Layout, out object layout);
            return page.Context.Pages.ResolveLayoutPage(page, layout as string);
        }

        public virtual object DefaultExpressionException(PageResult result, Exception ex)
        {
            if (result.Page.Context.RenderExpressionExceptions)
                return $"{ex.GetType().Name}: ${ex.Message}";
            
            // Evaluate Null References in Binding Expressions to null
            if (ScriptConfig.CaptureAndEvaluateExceptionsToNull.Contains(ex.GetType()))
                return JsNull.Value;

            return null;
        }

        public virtual async Task DefaultViewException(PageResult pageResult, IRequest req, Exception ex)
        {
            var sb = StringBuilderCache.Allocate();
            if (ContentType == MimeTypes.Html)
                sb.AppendLine("<pre class='error'>");
            sb.AppendLine($"{ex.GetType().Name}: {ex.Message}");
            if (pageResult.Context.DebugMode) 
                sb.AppendLine(ex.StackTrace);

            if (ex.InnerException != null)
            {
                sb.AppendLine();
                sb.AppendLine("Inner Exceptions:");
                var innerEx = ex.InnerException;
                while (innerEx != null)
                {
                    sb.AppendLine($"{innerEx.GetType().Name}: {innerEx.Message}");
                    if (pageResult.Context.DebugMode) 
                        sb.AppendLine(innerEx.StackTrace);
                    innerEx = innerEx.InnerException;;
                }
            }
            if (ContentType == MimeTypes.Html)
                sb.AppendLine("</pre>");
            var html = StringBuilderCache.ReturnAndFree(sb);
            await req.Response.OutputStream.WriteAsync(html);
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

            return str.HtmlEncode();
        }

        public SharpPage HtmlResolveLayout(SharpPage page)
        {
            var isCompletePage = page.BodyContents.Span.StartsWithIgnoreCase("<!DOCTYPE HTML>".AsSpan()) || page.BodyContents.Span.StartsWithIgnoreCase("<html".AsSpan());
            if (isCompletePage)
                return null;

            return base.DefaultResolveLayout(page);
        }
        
        public virtual object HtmlExpressionException(PageResult result, Exception ex)
        {
            if (result.Context.RenderExpressionExceptions)
                return ("<div class='error'><span>" + (ex.GetType().Name + ": " + ex.Message).HtmlEncode() + "</span></div>").ToRawString();
            
            // Evaluate Null References in Binding Expressions to null
            if (ScriptConfig.CaptureAndEvaluateExceptionsToNull.Contains(ex.GetType()))
                return JsNull.Value;

            return null;
        }

        public static async Task<Stream> HtmlEncodeTransformer(Stream stream)
        {
            var contents = await stream.ReadToEndAsync();
            var htmlEncoded = contents.HtmlEncode();
            return MemoryStreamFactory.GetStream(htmlEncoded.ToUtf8Bytes());
        }
    }

}