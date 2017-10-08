#if !NETSTANDARD2_0
using System.ServiceModel.Channels;

namespace ServiceStack
{
    public interface IRequiresSoapMessage
    {
        Message Message { get; set; }
    }
}
#endif
