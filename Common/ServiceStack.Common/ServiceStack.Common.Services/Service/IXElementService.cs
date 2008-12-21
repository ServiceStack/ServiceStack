using System.Xml.Linq;

namespace ServiceStack.Common.Services.Service
{
    public interface IXElementService 
    {
        object Execute(XElement xelementRequest);
    }
}