namespace ServiceStack
{
    public interface IServiceClient : IServiceClientAsync, IOneWayClient, IRestClient, IReplyClient, IHasSessionId, IHasVersion
    {
	}
}