namespace ServiceStack
{
	public interface IServiceClient : IServiceClientAsync, IOneWayClient
#if !(SL5 || __IOS__ || ANDROIDINDIE)
, IRestClient, IReplyClient
#endif
    {
	}

}