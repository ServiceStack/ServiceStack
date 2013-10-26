namespace ServiceStack
{
	public interface IServiceClient : IServiceClientAsync, IOneWayClient
#if !(SILVERLIGHT || MONOTOUCH || ANDROIDINDIE)
		, IRestClient, IReplyClient
#endif
    {
	}

}