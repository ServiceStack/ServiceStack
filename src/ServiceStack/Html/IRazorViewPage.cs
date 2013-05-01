using System.IO;
using ServiceStack.ServiceHost;

namespace ServiceStack.Html
{
    public interface IViewBag
    {
        bool TryGetItem(string name, out object result);
    }

    public interface IRazorViewPage
    {
        IViewBag TypedViewBag { get; }
        string Layout { get; set; }
        void SetChildPage(IRazorViewPage childPage, string childBody);
        IRazorViewPage ChildPage { get; }
        IRazorViewPage ParentPage { get; set; }
        void Init(IViewEngine viewEngine, IHttpRequest httpReq, IHttpResponse httpRes);
        void WriteTo(StreamWriter writer);
        void RenderSection(string sectionName, StreamWriter writer);
    }
}