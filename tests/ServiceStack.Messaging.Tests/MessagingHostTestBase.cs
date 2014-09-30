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

		[SetUp]
		public virtual void OnBeforeEachTest()
		{
			if (Container != null)
			{
				Container.Dispose();
			}

			Container = new Container();
			Container.Register(CreateMessageFactory());
		}

	}
}
