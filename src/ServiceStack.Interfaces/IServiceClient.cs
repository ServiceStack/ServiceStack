using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack
{
    public interface IServiceClient : IServiceClientAsync, IServiceGateway, IServiceGatewayAsync, IOneWayClient, IRestClient, IHasSessionId, IHasVersion
    {
    }

    public interface IJsonServiceClient : IServiceClient
    {
    }
}