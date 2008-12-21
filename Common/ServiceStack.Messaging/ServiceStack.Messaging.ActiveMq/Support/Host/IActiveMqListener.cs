namespace ServiceStack.Messaging.ActiveMq.Support.Host
{
    /// <summary>
    /// Internal methods required to interact with the running Active MQ ServiceHost
    /// </summary>
    public interface IActiveMqListener : IGatewayListener
    {
        /// <summary>
        /// Asserts that the service hosts connection is still connected.
        /// </summary>
        void AssertConnected();
    }
}