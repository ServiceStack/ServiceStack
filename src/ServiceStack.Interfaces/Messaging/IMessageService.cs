using System;
using System.Collections.Generic;

namespace ServiceStack.Messaging
{
    /// <summary>
    /// Simple definition of an MQ Host
    /// </summary>
    public interface IMessageService
        : IDisposable
    {
        /// <summary>
        /// Factory to create consumers and producers that work with this service
        /// </summary>
        IMessageFactory MessageFactory { get; }

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
        /// <param name="processExceptionEx"></param>
        void RegisterHandler<T>(Func<IMessage<T>, object> processMessageFn, Action<IMessageHandler, IMessage<T>, Exception> processExceptionEx);

        /// <summary>
        /// Get Total Current Stats for all Message Handlers
        /// </summary>
        /// <returns></returns>
        IMessageHandlerStats GetStats();

        /// <summary>
        /// Get a list of all message types registered on this MQ Host
        /// </summary>
        List<Type> RegisteredTypes { get; }

        /// <summary>
        /// Get the status of the service. Potential Statuses: Disposed, Stopped, Stopping, Starting, Started
        /// </summary>
        /// <returns></returns>
        string GetStatus();

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