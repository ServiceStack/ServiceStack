using System;

namespace ServiceStack.Redis
{
	public interface IRedisSubscription 
		: IDisposable
	{
		int SubscriptionCount { get; }
		
		Action<string> OnSubscribe { get; set; }
		Action<string, string> OnMessage { get; set; }
		Action<string> OnUnSubscribe { get; set; }

		void SubscribeToChannels(params string[] channels);
		void SubscribeToChannelsMatching(params string[] patterns);
		
		void UnSubscribeFromAllChannels();
		void UnSubscribeFromChannels(params string[] channels);
		void UnSubscribeFromChannelsMatching(params string[] patterns);
	}
}