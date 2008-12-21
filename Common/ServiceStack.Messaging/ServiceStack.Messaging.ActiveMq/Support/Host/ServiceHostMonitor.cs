using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using ServiceStack.Logging;

namespace ServiceStack.Messaging.ActiveMq.Support.Host
{
    public class ServiceHostMonitor : IResource
    {
        private const int FIRST_START_INTERVAL_MILLISECONDS = 1000;
        private readonly ILog log;
        private readonly TimeSpan interval;
        private readonly List<IActiveMqListener> activeMqlisteners;
        private Timer stateTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceHostMonitor"/> class.
        /// </summary>
        /// <param name="interval">The interval.</param>
        /// <param name="activeMqlisteners">The service hosts.</param>
        public ServiceHostMonitor(TimeSpan interval, List<IActiveMqListener> activeMqlisteners)
        {
            log = LogManager.GetLogger(GetType());
            this.interval = interval;
            this.activeMqlisteners = activeMqlisteners;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceHostMonitor"/> class.
        /// Helper constructor for services to create a ServiceHostMonitor from a list of IServiceHosts
        /// </summary>
        /// <param name="interval">The interval.</param>
        /// <param name="serviceHosts">The service hosts.</param>
        public ServiceHostMonitor(TimeSpan interval, List<IServiceHost> serviceHosts)
        {
            log = LogManager.GetLogger(GetType());
            this.interval = interval;
            this.activeMqlisteners = serviceHosts.ConvertAll<IActiveMqListener>(
                delegate (IServiceHost host) {
                     return (IActiveMqListener) host.GatewayListener;
                });
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
            log.InfoFormat("Starting ServiceHostMonitor, intervals at every {0} seconds", interval.TotalSeconds);
            AutoResetEvent autoEvent = new AutoResetEvent(false);
            TimerCallback timerDelegate = new TimerCallback(AssertServiceHostsAreConnected);
            TimeSpan firstInterval = TimeSpan.FromMilliseconds(FIRST_START_INTERVAL_MILLISECONDS);
            stateTimer = new Timer(timerDelegate, autoEvent, firstInterval, interval);
        }

        /// <summary>
        /// Asserts the service hosts are connected.
        /// </summary>
        /// <param name="state">The state.</param>
        public void AssertServiceHostsAreConnected(object state)
        {
            log.InfoFormat("Checking if all Service Hosts are connected.");
            foreach (IActiveMqListener host in activeMqlisteners)
            {
                try
                {
                    host.AssertConnected();
                }
                catch (Exception ex)
                {
                    IPHostEntry IPHost = Dns.GetHostEntry(Dns.GetHostName());
                    log.ErrorFormat("ActiveMq ServiceHost running on '{0}' that is listening to '{1}' has failed: {2}",
                        IPHost.AddressList[0], host.Destination.Uri, ex.Message);
                }
            }
        }

        #region IDisposable Members

        /// <summary>
        /// Disposes of the stateTimer
        /// </summary>
        public void Dispose()
        {
            if (stateTimer != null)
            {
                stateTimer.Dispose();
            }
        }

        #endregion
    }
}