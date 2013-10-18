namespace ServiceStack
{
	public interface IServiceClient : IServiceClientAsync, IOneWayClient, IRestClient
#if !(SILVERLIGHT || MONOTOUCH || ANDROIDINDIE)
		, IReplyClient
#endif
	{
	}

}