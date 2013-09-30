namespace ServiceStack
{
	public interface IServiceClient : IServiceClientAsync, IOneWayClient
#if !(SILVERLIGHT || MONOTOUCH || ANDROIDINDIE)
		, IReplyClient
#endif
	{
	}

}