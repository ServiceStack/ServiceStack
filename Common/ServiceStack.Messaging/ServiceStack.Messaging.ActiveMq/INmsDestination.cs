namespace ServiceStack.Messaging.ActiveMq
{
    public interface INmsDestination : IDestination
    {
        /// <summary>
        /// Gets the NMS destination.
        /// </summary>
        /// <value>The NMS destination.</value>
        NMS.IDestination NmsDestination { get; }
    }
}