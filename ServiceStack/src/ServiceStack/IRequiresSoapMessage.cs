#if !NETCORE
using System.ServiceModel.Channels;

namespace ServiceStack;

public interface IRequiresSoapMessage
{
    Message Message { get; set; }
}

#endif
