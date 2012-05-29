using System;
using ServiceStack.Messaging;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface
{
    /// <summary>
    /// Useful base functionality for IAsyncServices by serializing the request
    /// into the message queue configured by the AppHost if one is configured. 
    /// 
    /// This allows the request to persist for longer than the request duration 
    /// and can defer the execution of the async request under optimal execution.
    /// 
    /// If one is not configured it will Execute the request immediately as per normal.
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    [Obsolete("IAsyncService hae been merged into ServiceBase")]
    public abstract class AsyncServiceBase<TRequest> 
        : ServiceBase<TRequest>, IAsyncService<TRequest>
    {
        /// <summary>
        /// Persists the request into the registered message queue if configured, 
        /// otherwise calls Execute() to handle the request immediately.
        /// </summary>
        /// <param name="request"></param>
        public override object ExecuteAsync(TRequest request)
        {
            if (MessageFactory == null)
            {
                return Execute(request);
            }

            //Capture and persist this async request on this Services 'In Queue' 
            //for execution after this request has been completed
            using (var producer = MessageFactory.CreateMessageProducer())
            {
                producer.Publish(request);
            }

            return ServiceUtils.CreateResponseDto(request);
        }

        /// <summary>
        /// The Deferred execution of ExecuteAsync(request)'s. 
        /// This request is typically invoked from a messaging queue service host.
        /// </summary>
        /// <param name="request"></param>
        public virtual object ExecuteAsync(IMessage<TRequest> request)
        {
            return Run(request.GetBody());
        }

    }


}