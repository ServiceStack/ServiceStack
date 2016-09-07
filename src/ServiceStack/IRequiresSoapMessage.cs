#if !NETSTANDARD1_3
using System.ServiceModel.Channels;

namespace ServiceStack
{
    public interface IRequiresSoapMessage
    {
        Message Message { get; set; }
    }
}
#endif
