namespace ServiceStack.Service
{
	public interface IServiceClient : IServiceClientAsync, IOneWayClient, IMayRequireCredentials
#if !(SILVERLIGHT || MONOTOUCH)
		, IReplyClient
#endif
	{
	}
}