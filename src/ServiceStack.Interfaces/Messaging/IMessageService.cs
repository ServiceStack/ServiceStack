using System;

namespace ServiceStack.Messaging
{
    /// <summary>
    /// Simple definition of an MQ Host
    /// </summary>
	public interface IMessageService
		: IDisposable
	{
        /// <summary>
        /// Register DTOs and hanlders the MQ Host will process
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="processMessageFn"></param>
		void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn);

        /// <summary>
        /// Register DTOs and hanlders the MQ Host will process
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="processMessageFn"></param>
        void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, Action<Exception> processExceptionEx);

        /// <summary>
        /// Get Total Current Stats for all Message Handlers
        /// </summary>
        /// <returns></returns>
        IMessageHandlerStats GetStats();
        
        /// <summary>
        /// Get a Stats dump
        /// </summary>
        /// <returns></returns>
        string GetStatsDescription();

        /// <summary>
        /// Start the MQ Host if not already started.
        /// </summary>
		void Start();

        /// <summary>
        /// Stop the MQ Host if not already stopped. 
        /// </summary>
        void Stop();
	}

}