using System;
using System.Collections.Generic;
using System.Threading;
using ServiceStack.Messaging.ActiveMq;
using ServiceStack.Messaging.ActiveMq.Support.Host;
using NUnit.Framework;
using Rhino.Mocks;

namespace ServiceStack.Messaging.Tests.UnitTests
{
    [TestFixture]
    public class ServiceHostMonitorTests : UnitTestCaseBase
    {
        [Test]
        public void ServiceHostMonitor_ListenersTest()
        {
            IActiveMqListener listener = Mock.CreateMock<IActiveMqListener>();
            List<IActiveMqListener> listeners = new List<IActiveMqListener>();
            listeners.Add(listener);

            Expect.Call(delegate { listener.AssertConnected(); });
            ReplayAll();

            ServiceHostMonitor monitor = new ServiceHostMonitor(TimeSpan.FromSeconds(1), listeners);
            monitor.Start();

            //give some extra time for the monitor to perform its first iteration
            Thread.Sleep(TimeSpan.FromSeconds(1.5));

            VerifyAll();
        }

        [Test]
        public void ServiceHostMonitor_ServiceHostsTest()
        {
            IActiveMqListener listener = Mock.CreateMock<IActiveMqListener>();
            List<IServiceHost> listeners = new List<IServiceHost>();
            listeners.Add(new ActiveMqServiceHost(listener, new ActiveMqServiceHostConfigQueue()));

            Expect.Call(delegate { listener.AssertConnected(); });
            ReplayAll();

            ServiceHostMonitor monitor = new ServiceHostMonitor(TimeSpan.FromSeconds(1), listeners);
            monitor.Start();

            //give some extra time for the monitor to perform its first iteration
            Thread.Sleep(TimeSpan.FromSeconds(1.5));

            VerifyAll();
        }
    }
}