using ServiceStack.ServiceInterface;

namespace ServiceStack.Messaging.Tests.Services
{
	public abstract class MessagingServiceBase<T>
		: AsyncServiceBase<T>
	{
	}

}