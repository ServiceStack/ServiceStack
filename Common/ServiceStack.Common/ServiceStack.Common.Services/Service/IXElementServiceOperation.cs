using System.Xml.Linq;

namespace ServiceStack.Common.Services.Service
{
    public interface IXElementServiceOperation : IServiceOperation
    {
        object Execute(XElement request);
    }
}