using System;
using System.Web;

namespace ServiceStack.MiniProfiler
{
    public enum RenderPosition
    {
        Left = 0,
        Right = 1
    }

    public interface IProfiler
    {
        IProfiler Start();
        void Stop();

        IDisposable Step(string name);

        IHtmlString RenderIncludes(RenderPosition? position = null, bool? showTrivial = null,
            bool? showTimeWithChildren = null, int? maxTracesToShow = null, bool xhtml = false,
            bool? showControls = null);
    }

    public class NullProfiler : IProfiler
    {
        public static readonly NullProfiler Instance = new NullProfiler();

        readonly MockDisposable disposable = new MockDisposable();

        class MockDisposable : IDisposable
        {
            public void Dispose() { }
        }

        public IProfiler Start() => this;
        public void Stop() { }
        public IDisposable Step(string name) => disposable;

        public IHtmlString RenderIncludes(RenderPosition? position = null, bool? showTrivial = null,
            bool? showTimeWithChildren = null,
            int? maxTracesToShow = null, bool xhtml = false, bool? showControls = null)
        {
            return HtmlString.Empty;
        }
    }

    public static class Profiler
    {
        public static IProfiler Current { get; set; } = NullProfiler.Instance;

        public static IDisposable Step(string name) => Current.Step(name);

        public static IHtmlString RenderIncludes(RenderPosition? position = null, bool? showTrivial = null,
            bool? showTimeWithChildren = null,
            int? maxTracesToShow = null, bool xhtml = false, bool? showControls = null)
        {
            return Current.RenderIncludes(position, showTrivial, showTimeWithChildren, maxTracesToShow, xhtml,
                showControls);
        }

        public static IProfiler Start() => Current.Start();
        public static void Stop() => Current.Stop();
    }

    public class HtmlString : IHtmlString, System.Web.IHtmlString
    {
        public static HtmlString Empty = new HtmlString(string.Empty);

        private readonly string value;

        public HtmlString(string value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return value;
        }

        public string ToHtmlString()
        {
            return this.ToString();
        }
    }
}

namespace ServiceStack.Html
{
    public static class HtmlStringExtensions
    {
        public static System.Web.IHtmlString AsRaw(this IHtmlString htmlString) => htmlString is System.Web.IHtmlString aspRawStr
            ? aspRawStr
            : new HtmlString(htmlString?.ToHtmlString() ?? "");
    }
}