#if NETSTANDARD1_6

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ServiceStack.Support.Markdown;
using ServiceStack.Web;

namespace System.Threading
{
    public static class ThreadExtensions
    {
        public static void Abort(this Thread thread)
        {
            MethodInfo abort = null;
            foreach (var m in thread.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (m.Name.Equals("AbortInternal") && m.GetParameters().Length == 0) abort = m;
            }
            if (abort == null)
                throw new NotImplementedException();

            abort.Invoke(thread, new object[0]);
        }

        public static void Interrupt(this Thread thread)
        {
            MethodInfo interrupt = null;
            foreach (var m in thread.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (m.Name.Equals("InterruptInternal") && m.GetParameters().Length == 0) interrupt = m;
            }
            if (interrupt == null)
                throw new NotImplementedException();

            interrupt.Invoke(thread, new object[0]);
        }

        public static bool Join(this Thread thread, TimeSpan timeSpan)
        {
            return thread.Join((int)timeSpan.TotalMilliseconds);
        }
    }
}

namespace System.Configuration
{
    public class ConfigurationErrorsException : Exception
    {
        public ConfigurationErrorsException() {}
        public ConfigurationErrorsException(string message) : base(message) {}
        public ConfigurationErrorsException(string message, Exception innerException) 
            : base(message, innerException) {}
    }
}

namespace ServiceStack.MiniProfiler
{
    public enum RenderPosition { Left = 0, Right = 1 }

    public class Profiler
    {
        public static Profiler Current { get; } = new Profiler();
        readonly MockDisposable disposable = new MockDisposable();

        class MockDisposable : IDisposable
        {
            public void Dispose() {}
        }

        public IDisposable Step(string deserializeRequest)
        {
            return disposable;
        }

        public static IHtmlString RenderIncludes(RenderPosition? position = null, bool? showTrivial = null, bool? showTimeWithChildren = null, int? maxTracesToShow = null, bool xhtml = false, bool? showControls = null)
        {
            return new HtmlString(string.Empty);
        }
    }
}

namespace ServiceStack.Html
{
    public class HtmlHelper<T> : HtmlHelper {}

    public class HtmlHelper
    {
        internal bool RenderHtml { get; private set; }

        public IHttpRequest HttpRequest { get; set; }
        public IHttpResponse HttpResponse { get; set; }
        public StreamWriter Writer { get; set; }
        public IViewEngine ViewEngine { get; set; }

        public IRazorView RazorPage { get; protected set; }
        public MarkdownPage MarkdownPage { get; protected set; }
        public Dictionary<string, object> ScopeArgs { get; protected set; }
        private ViewDataDictionary viewData;
        public ViewDataDictionary ViewData
        {
            get { return viewData ?? (viewData = new ViewDataDictionary()); }
            protected set { viewData = value; }
        }

        public void Init(IViewEngine viewEngine, IRequest httpReq, IResponse httpRes, IRazorView razorPage,
            Dictionary<string, object> scopeArgs = null, ViewDataDictionary viewData = null)
        {
            ViewEngine = viewEngine;
            HttpRequest = httpReq as IHttpRequest;
            HttpResponse = httpRes as IHttpResponse;
            RazorPage = razorPage;
            //ScopeArgs = scopeArgs;
            this.viewData = viewData;
        }
        public void Init(MarkdownPage markdownPage, Dictionary<string, object> scopeArgs,
            bool renderHtml, ViewDataDictionary viewData, HtmlHelper htmlHelper)
        {
            Init(null, null, markdownPage.Markdown, viewData, htmlHelper);

            this.RenderHtml = renderHtml;
            this.MarkdownPage = markdownPage;
            this.ScopeArgs = scopeArgs;
        }

        public void Init(IHttpRequest httpReq, IHttpResponse httpRes, IViewEngine viewEngine, ViewDataDictionary viewData, HtmlHelper htmlHelper)
        {
            this.RenderHtml = true;
            this.HttpRequest = httpReq ?? htmlHelper?.HttpRequest;
            this.HttpResponse = httpRes ?? htmlHelper?.HttpResponse;
            this.ViewEngine = viewEngine;
            this.ViewData = viewData;
            this.ViewData.PopulateModelState();
        }

        public MvcHtmlString Partial(string viewName)
        {
            return Partial(viewName, null);
        }

        public MvcHtmlString Partial(string viewName, object model)
        {
            var masterModel = this.viewData;
            try
            {
                this.viewData = new ViewDataDictionary(model);
                var result = ViewEngine.RenderPartial(viewName, model, this.RenderHtml, Writer, this);
                return MvcHtmlString.Create(result);
            }
            finally
            {
                this.viewData = masterModel;
            }
        }
        public MvcHtmlString Raw(object content)
        {
            if (content == null) return null;
            var strContent = content as string;
            return MvcHtmlString.Create(strContent ?? content.ToString()); //MvcHtmlString
        }

        public static string GenerateIdFromName(string name)
        {
            return GenerateIdFromName(name, TagBuilder.IdAttributeDotReplacement);
        }

        public static string GenerateIdFromName(string name, string idAttributeDotReplacement)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (idAttributeDotReplacement == null)
                throw new ArgumentNullException(nameof(idAttributeDotReplacement));

            return name.Length == 0 ? string.Empty : TagBuilder.CreateSanitizedId(name, idAttributeDotReplacement);
        }
    }

    public static class HtmlHelperExtensions
    {
        public static IRequest GetHttpRequest(this HtmlHelper html)
        {
            return html?.HttpRequest;
        }
    }

}

namespace ServiceStack.Platforms
{
    public partial class PlatformNetCore : Platform
    {

    }
}

#endif
