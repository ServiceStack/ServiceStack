using NUnit.Framework;

namespace ServiceStack.Messaging.Tests
{
	[TestFixture]
	public abstract class MessagingHostTestBase
	{
		protected abstract MessagingServiceBase CreateMessagingService();
	}
}
