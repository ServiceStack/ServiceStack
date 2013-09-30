using NUnit.Framework;
using ServiceStack.Messaging;
using ServiceStack.Messaging.Redis;
using ServiceStack.Redis;

namespace ServiceStack.Server.Tests.Messaging
{
    [Category("Integration")]
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