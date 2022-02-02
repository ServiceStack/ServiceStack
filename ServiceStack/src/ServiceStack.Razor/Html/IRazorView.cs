using System;
using System.IO;
using ServiceStack.Web;

namespace ServiceStack.Html
{
    public interface IViewBag
    {
        bool TryGetItem(string name, out object result);
    }

    public interface IRazorView : IDisposable
    {
        IViewBag TypedViewBag { get; }
        string Layout { get; set; }
        void SetChildPage(IRazorView childPage, string childBody);
        IRazorView ChildPage { get; }
        IRazorView ParentPage { get; set; }
        void Init(IViewEngine viewEngine, IRequest httpReq, IResponse httpRes);
        void WriteTo(StreamWriter writer);
        bool IsSectionDefined(string sectionName);
        void RenderChildSection(string sectionName, StreamWriter writer);
    }
}