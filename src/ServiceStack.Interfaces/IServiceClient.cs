namespace ServiceStack
{
    public interface IServiceClient : IReplyClient, IOneWayClient, IRestClient, IHasSessionId, IHasVersion
#if !UNITY
        , IServiceClientAsync
#endif
    {
    }

    public interface IJsonServiceClient : IServiceClient {}

    public interface IReplyClient : IServiceGateway { }

#if !UNITY
    public interface IServiceClientAsync : IServiceGatewayAsync, IRestClientAsync {}
#endif
}