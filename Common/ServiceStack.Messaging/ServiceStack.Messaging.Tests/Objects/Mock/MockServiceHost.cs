using System;

namespace ServiceStack.Messaging.Tests.Objects.Mock
{
    public class MockServiceHost : IServiceHost
    {
        private readonly IGatewayListener gatewayListener;
        private readonly IServiceHostConfig config;

        public MockServiceHost(IGatewayListener gatewayListener, IServiceHostConfig config)
        {
            this.gatewayListener = gatewayListener;
            this.config = config;
        }

        public IGatewayListener GatewayListener
        {
            get { return gatewayListener; }
        }

        public IServiceHostConfig Config
        {
            get { return config; }
        }

        public IConnection Connection
        {
            get { return gatewayListener.Connection; }
        }

        public IService CreateInstance()
        {
            return (IService)Activator.CreateInstance(config.ServiceType);
        }
    }
}