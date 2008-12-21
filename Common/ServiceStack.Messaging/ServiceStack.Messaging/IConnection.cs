using System;

namespace ServiceStack.Messaging
{
    public interface IConnection : IDisposable
    {
        /// <summary>
        /// Creates the client.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <returns></returns>
        IOneWayClient CreateClient(IDestination destination);

        /// <summary>
        /// Creates the client.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <returns></returns>
        IReplyClient CreateReplyClient(IDestination destination);

        /// <summary>
        /// Creates the listener.
        /// </summary>
        IGatewayListener CreateListener(IDestination destination);

        /// <summary>
        /// Creates the registered listener.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="subscriberId">The subscriber id.</param>
        /// <returns></returns>
        IRegisteredListener CreateRegisteredListener(IDestination destination, string subscriberId);

        /// <summary>
        /// Creates the text message.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        ITextMessage CreateTextMessage(string text);
    }
}
