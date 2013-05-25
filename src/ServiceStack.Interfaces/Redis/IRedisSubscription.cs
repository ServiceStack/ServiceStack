using System;

namespace ServiceStack.Redis
{
	public interface IRedisSubscription 
		: IDisposable
	{
		/// <summary>
		/// The number of active subscriptions this client has
		/// </summary>
		long SubscriptionCount { get; }
		
		/// <summary>
		/// Registered handler called after client *Subscribes* to each new channel
		/// </summary>
		Action<string> OnSubscribe { get; set; }
		
		/// <summary>
		/// Registered handler called when each message is received
		/// </summary>
		Action<string, string> OnMessage { get; set; }

		/// <summary>
		/// Registered handler called when each channel is unsubscribed
		/// </summary>
		Action<string> OnUnSubscribe { get; set; }

		/// <summary>
		/// Subscribe to channels by name
		/// </summary>
		/// <param name="channels"></param>
		void SubscribeToChannels(params string[] channels);

		/// <summary>
		/// Subscribe to channels matching the supplied patterns
		/// </summary>
		/// <param name="patterns"></param>
		void SubscribeToChannelsMatching(params string[] patterns);
		
		void UnSubscribeFromAllChannels();
		void UnSubscribeFromChannels(params string[] channels);
		void UnSubscribeFromChannelsMatching(params string[] patterns);
	}
}