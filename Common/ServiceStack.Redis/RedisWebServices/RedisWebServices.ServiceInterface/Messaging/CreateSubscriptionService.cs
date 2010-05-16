using System.Threading;
using RedisWebServices.ServiceModel.Operations.Messaging;
using ServiceStack.Text;

namespace RedisWebServices.ServiceInterface.Messaging
{
	public class CreateSubscriptionService
		: RedisServiceBase<CreateSubscription>
	{
		protected override object Run(CreateSubscription request)
		{
			string key = null;
			string message = null;

			using (var redisClient = base.ClientsManager.GetClient())
			using (var subscription = redisClient.CreateSubscription())
			{
				var isActive = true;

				subscription.OnSubscribe = k =>
				{
					if (request.TimeOut.HasValue)
					{
						ThreadPool.QueueUserWorkItem(x =>
						{
							Thread.Sleep(request.TimeOut.Value);
							if (isActive != null)
							{
								subscription.UnSubscribeFromAllChannels();
							}
						});
					}
				};

				subscription.OnMessage = (k, msg) =>
				{
					key = k;
					message = msg;

					subscription.UnSubscribeFromAllChannels();
				};

				if (!request.Channels.IsNullOrEmpty())
				{
					subscription.SubscribeToChannels(request.Channels.ToArray());
				}
				else if (!request.Patterns.IsNullOrEmpty())
				{
					subscription.SubscribeToChannelsMatching(request.Patterns.ToArray());
				}

				isActive = false;
			}

			return new CreateSubscriptionResponse
			{
				Key = key,
				Message = message
			};
		}
	}
}