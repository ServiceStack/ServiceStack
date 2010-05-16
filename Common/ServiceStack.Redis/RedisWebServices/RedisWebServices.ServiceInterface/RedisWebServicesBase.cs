using System;
using ServiceStack.Redis;
using ServiceStack.ServiceInterface;

namespace RedisWebServices.ServiceInterface
{
	/// <summary>
	/// Common base class for all Redis Web Services
	/// </summary>
	/// <typeparam name="TRequest"></typeparam>
	public abstract class RedisServiceBase<TRequest>
		: ServiceBase<TRequest>
	{
		public AppConfig Config { get; set; }

		public IRedisClientsManager ClientsManager { get; set; }

		protected void RedisExec(Action<IRedisClient> redisFn)
		{
			using (var redisClient = ClientsManager.GetClient())
			{
				redisFn(redisClient);
			}
		}

		protected T RedisExec<T>(Func<IRedisClient, T> redisFn)
		{
			using (var redisClient = ClientsManager.GetClient())
			{
				return redisFn(redisClient);
			}
		}

		protected T RedisNativeExec<T>(Func<IRedisNativeClient, T> redisFn)
		{
			using (var redisClient = ClientsManager.GetClient())
			{
				return redisFn((IRedisNativeClient)redisClient);
			}
		}

		protected void RedisNativeExec(Action<IRedisNativeClient> redisFn)
		{
			using (var redisClient = ClientsManager.GetClient())
			{
				redisFn((IRedisNativeClient)redisClient);
			}
		}
	}
}