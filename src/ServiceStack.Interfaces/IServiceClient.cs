using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack
{
    public interface IServiceClient : IServiceClientAsync, IReplyClient, IOneWayClient, IRestClient, IHasSessionId, IHasVersion
    {
    }

    public interface IJsonServiceClient : IServiceClient {}

    public interface IReplyClient : IServiceGateway { }

    public interface IServiceClientAsync : IServiceGatewayAsync, IRestClientAsync {}
}