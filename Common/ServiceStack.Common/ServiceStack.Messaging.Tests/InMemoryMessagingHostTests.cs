using System;

namespace ServiceStack.Messaging.Tests
{
	public class InMemoryMessagingHostTests
		: BasicServiceMessagingTests
	{
		protected override MessagingServiceBase CreateMessagingService()
		{
			return new InMemoryMessagingService();
		}
	}
}