using DependencyInjection;
using Funke;
using NUnit.Framework;
using ServiceStack.Messaging.Tests.Services;

namespace ServiceStack.Messaging.Tests
{
	[TestFixture]
	public abstract class MessagingHostTestBase
	{
		protected abstract IMessageFactory CreateMessageFactory();

		protected abstract TransientMessageServiceBase CreateMessagingService();

		protected Container Container { get; set; }
        protected DependencyInjector DependencyInjector { get; set; }

		[SetUp]
		public virtual void OnBeforeEachTest()
		{
			if (DependencyInjector != null)
			{
				DependencyInjector.Dispose();
			}

			DependencyInjector = new DependencyInjector();
			DependencyInjector.Register(CreateMessageFactory());
		}

	}
}
