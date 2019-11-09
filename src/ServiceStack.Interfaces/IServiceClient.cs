using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack
{
    public interface IServiceClient : IServiceClientAsync, IHttpRestClientAsync, IReplyClient, IOneWayClient, IRestClient, IHasSessionId, IHasBearerToken, IHasVersion
    {
    }

    public interface IJsonServiceClient : IServiceClient {}

    public interface IReplyClient : IServiceGateway { }

    public interface IServiceClientCommon
    {
        void SetCredentials(string userName, string password);
    }

    public interface IServiceClientAsync : IServiceGatewayAsync, IRestClientAsync {}
    public interface IServiceClientSync : IServiceGateway, IRestClientSync {}
}