using ServiceStack.Messaging;
using ServiceStack.Messaging.Tests;
using ServiceStack.Redis.Messaging;

namespace ServiceStack.Redis.Tests.Messaging
{
	public class RedisTransientMessagingHostTests
		: TransientServiceMessagingTests
	{
		private IRedisClientsManager clientManager;
		private RedisTransientMessageFactory factory;

		public override void OnBeforeEachTest()
		{
			ResetConnections();

			using (var client = clientManager.GetClient())
			{
				client.FlushAll();
			}

			base.OnBeforeEachTest();
		}

		protected override IMessageFactory CreateMessageFactory()
		{
			return factory;
		}

		protected override TransientMessageServiceBase CreateMessagingService()
		{
			return factory.MessageService;
		}

		private void ResetConnections()
		{
			if (clientManager != null)
			{
				clientManager.Dispose();
				clientManager = null;
			}

			if (factory != null)
			{
				factory.Dispose();
				factory = null;
			}

			clientManager = new BasicRedisClientManager(TestConfig.MasterHosts);
			factory = new RedisTransientMessageFactory(clientManager);
		}
	}
}