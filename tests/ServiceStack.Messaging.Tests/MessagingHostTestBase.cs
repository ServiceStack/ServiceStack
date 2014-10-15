using NUnit.Framework;
using ServiceStack.DependencyInjection;
using ServiceStack.Messaging.Tests.Services;

namespace ServiceStack.Messaging.Tests
{
	[TestFixture]
	public abstract class MessagingHostTestBase
	{
		protected abstract IMessageFactory CreateMessageFactory();

		protected abstract TransientMessageServiceBase CreateMessagingService();

        protected DependencyService DependencyService { get; set; }

		[SetUp]
		public virtual void OnBeforeEachTest()
		{
			DependencyService = new DependencyService();
			DependencyService.Register(CreateMessageFactory());
		}

	}
}
