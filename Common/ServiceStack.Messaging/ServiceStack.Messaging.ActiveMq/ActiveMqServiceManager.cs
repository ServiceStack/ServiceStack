using System;
using System.Collections.Generic;
using System.Diagnostics;
using ServiceStack.Logging;
using ServiceStack.Messaging.ActiveMq.Serialization;
using ServiceStack.Messaging.ActiveMq.Support.Logging;

namespace ServiceStack.Messaging.ActiveMq
{
    /// <summary>
    /// Message Queue service class. Encapsulates the functionality required to host Message Queue listeners
    /// inside a Windows NT Service. Queue and Topic Subscribers can be attached to invoke services.
    /// </summary>
    public class ActiveMqServiceManager : IServiceManager
    {
        private readonly ILog log;
        private const string EVENT_LOG_SOURCE = "Application";
        private const string TEXT_MESSAGE_REQUEST_TYPE = "TextMessage";
        private bool isDisposed;
        private const string DEFAULT_SECTION_NAME = "serviceHosts";
        private readonly List<IServiceHost> serviceHosts;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveMqServiceManager"/> class with the configuration
        /// specified in the provided configSectionName.
        /// Default is &lt;serviceHosts /&gt;
        /// </summary>
        /// <param name="serviceHosts">The service hosts.</param>
        public ActiveMqServiceManager(List<IServiceHost> serviceHosts)
        {
            this.serviceHosts = serviceHosts;
            log = LogManager.GetLogger(GetType());
            ActiveMQ.Tracer.Trace = new ActiveMqTracer();
            isDisposed = false;
        }

        /// <summary>
        /// Starts all the queue and topic subscribers specified in the App.config file
        /// </summary>
        public void Start()
        {
            foreach (IServiceHost serviceHost in serviceHosts)
            {
                serviceHost.GatewayListener.MessageReceived += Listener_MessageReceived;
                serviceHost.GatewayListener.Start();
            }
        }

        /// <summary>
        /// Processes a Soap Message. The message is eventually processed by the IService or ITextService specified.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        void Listener_MessageReceived(object source, MessageEventArgs e)
        {
            try
            {
                IGatewayListener listener = (IGatewayListener)source;
                IServiceHost serviceHost = GetListenersServiceHost(listener);
                if (serviceHost == null)
                {
                    throw new ApplicationException("could not find the listeners service host");
                }
                string responseText = null;
                string requestType = null;
                string requestBody = null;

                try
                {
                    responseText = ExecuteService(serviceHost, e.Message);
                    serviceHost.GatewayListener.Commit();
                }
                catch (Exception ex)
                {
                    log.Error("Error occured while executing service.", ex);
                    serviceHost.GatewayListener.Rollback();
                    if (e.Message.ReplyTo != null)
                    {
                        SoapFault soapFault = new SoapFault(requestType, ex, requestBody);
                        responseText = soapFault.ToString();
                    }
                }
                if (e.Message.ReplyTo != null)
                {
                    ITextMessage responseMessage = listener.Connection.CreateTextMessage(responseText);
                    responseMessage.CorrelationId = e.Message.CorrelationId;
                    responseMessage.To = e.Message.ReplyTo;
                    SendMessage(serviceHost, responseMessage);
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message + " \n\n Stack Trace:\n" + ex.StackTrace);
            }
        }

        /// <summary>
        /// Helper method used to send the message provided to the specified destination.
        /// </summary>
        /// <param name="serviceHost">The service host.</param>
        /// <param name="message">The message.</param>
        private static void SendMessage(IServiceHost serviceHost, ITextMessage message)
        {
            using (IOneWayClient client = serviceHost.Connection.CreateClient(message.To))
            {
                client.SendOneWay(message);
            }
        }

        /// <summary>
        /// Executes the specified IService with the sent serializable object received.
        /// </summary>
        /// <param name="serviceHost">The service host.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        private string ExecuteService(IServiceHost serviceHost, ITextMessage message)
        {
            IService service = serviceHost.CreateInstance();
            log.InfoFormat("Received Text Message of {0} length", message.Text.Length);
            return service.Execute(serviceHost, message);
        }

        /// <summary>
        /// Gets the listeners' service host.
        /// </summary>
        /// <param name="listener">The listener.</param>
        /// <returns></returns>
        private IServiceHost GetListenersServiceHost(IGatewayListener listener)
        {
            foreach (IServiceHost serviceHost in serviceHosts)
            {
                if (serviceHost.GatewayListener == listener)
                {
                    return serviceHost;
                }
            }
            return null;
        }

        /// <summary>
        /// Stops all the subscribers from receiving any more messages.
        /// </summary>
        public void Stop()
        {
            Dispose();
            Debug.WriteLine("All ActiveMq Service Hosts stopped");
        }


        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="ActiveMqServiceManager"/> is reclaimed by garbage collection.
        /// </summary>
        ~ActiveMqServiceManager()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (isDisposed)
            {
                return;
            }
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }

            isDisposed = true;
            foreach (IServiceHost serviceHost in serviceHosts)
            {
                try
                {
                    if (serviceHost != null)
                    {
                        serviceHost.GatewayListener.Dispose();
                    }
                }
                catch (Exception e)
                {
                    log.ErrorFormat("Error disposing of serviceHost: {0}", e.Message);
                }
            }
        }
    }
}
