namespace ServiceStack.Clients
{
	public interface IServiceClient : IServiceClientAsync, IOneWayClient
#if !(SILVERLIGHT || MONOTOUCH || ANDROIDINDIE)
		, IReplyClient
#endif
	{
	}

}