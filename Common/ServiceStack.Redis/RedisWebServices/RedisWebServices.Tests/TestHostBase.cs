using Funq;
using RedisWebServices.ServiceInterface;
using RedisWebServices.ServiceInterface.Admin;
using ServiceStack.Configuration;
using ServiceStack.Redis;
using ServiceStack.WebHost.Endpoints;

namespace RedisWebServices.Tests
{
	public class TestHost
		: AppHostBase
	{
		public TestHost()
			: base("Redis Web Services", typeof(PingService).Assembly)
		{
		}

		public override void Configure(Container container)
		{
			container.Register(c => new AppConfig(new ConfigurationResourceManager()));
			var config = container.Resolve<AppConfig>();

			container.Register<IRedisClientsManager>(c =>
				new BasicRedisClientManager(config.RedisHostAddress));
		}
	}
}
