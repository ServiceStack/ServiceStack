namespace ServiceStack
{
	public interface IServiceClient : IServiceClientAsync, IOneWayClient
#if !(SL5 || IOS || ANDROIDINDIE)
		, IRestClient, IReplyClient
#endif
    {
	}

}