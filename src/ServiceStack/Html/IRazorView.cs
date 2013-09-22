using System.IO;
using ServiceStack.Server;
using ServiceStack.ServiceHost;

namespace ServiceStack.Html
{
    public interface IViewBag
    {
        bool TryGetItem(string name, out object result);
    }

    public interface IRazorView
    {
        IViewBag TypedViewBag { get; }
        string Layout { get; set; }
        void SetChildPage(IRazorView childPage, string childBody);
        IRazorView ChildPage { get; }
        IRazorView ParentPage { get; set; }
        void Init(IViewEngine viewEngine, IHttpRequest httpReq, IHttpResponse httpRes);
        void WriteTo(StreamWriter writer);
        bool IsSectionDefined(string sectionName);
        void RenderSection(string sectionName, StreamWriter writer);
    }
}