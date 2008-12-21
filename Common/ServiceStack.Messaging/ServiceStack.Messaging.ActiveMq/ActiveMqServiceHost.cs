using System;

namespace ServiceStack.Messaging.ActiveMq
{
    public class ActiveMqServiceHost : IServiceHost 
    {
        private readonly IGatewayListener gatewayListener;
        private readonly IServiceHostConfig hostConfig;

        public ActiveMqServiceHost(IGatewayListener gatewayListener, IServiceHostConfig serviceHostConfig)
        {
            this.gatewayListener = gatewayListener;
            this.hostConfig = serviceHostConfig;
        }

        #region IServiceHost Members

        public IGatewayListener GatewayListener
        {
            get { return gatewayListener; }
        }

        public IServiceHostConfig Config
        {
            get { return hostConfig; }
        }

        public IService CreateInstance()
        {
            return (IService) Activator.CreateInstance(hostConfig.ServiceType);
        }

        public IConnection Connection
        {
            get
            {
                return gatewayListener.Connection;
            }
        }

        #endregion
    }
}
