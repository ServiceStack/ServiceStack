#if !NETSTANDARD1_6
using System.ServiceModel.Channels;

namespace ServiceStack
{
    public interface IRequiresSoapMessage
    {
        Message Message { get; set; }
    }
}
#endif
