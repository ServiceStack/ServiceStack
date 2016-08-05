using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack
{
    public abstract class ServiceGatewayFactoryBase : IServiceGatewayFactory, IServiceGateway, IServiceGatewayAsync
    {
        public IRequest Request { get; private set; }

        protected InProcessServiceGateway localGateway;

        public virtual IServiceGateway GetServiceGateway(IRequest request)
        {
            this.Request = request;
            localGateway = new InProcessServiceGateway(request);
            return this;
        }

        public abstract IServiceGateway GetGateway(Type requestType);

        protected virtual IServiceGatewayAsync GetGatewayAsync(Type requestType)
        {
            return (IServiceGatewayAsync)GetGateway(requestType);
        }

        public TResponse Send<TResponse>(object requestDto)
        {
            return GetGateway(requestDto.GetType()).Send<TResponse>(requestDto);
        }

        public List<TResponse> SendAll<TResponse>(IEnumerable<object> requestDtos)
        {
            return GetGateway(requestDtos.GetType().GetCollectionType()).SendAll<TResponse>(requestDtos);
        }

        public void Publish(object requestDto)
        {
            GetGateway(requestDto.GetType()).Publish(requestDto);
        }

        public void PublishAll(IEnumerable<object> requestDtos)
        {
            GetGateway(requestDtos.GetType().GetCollectionType()).PublishAll(requestDtos);
        }

        public Task<TResponse> SendAsync<TResponse>(object requestDto, CancellationToken token = new CancellationToken())
        {
            return GetGatewayAsync(requestDto.GetType()).SendAsync<TResponse>(requestDto, token);
        }

        public Task<List<TResponse>> SendAllAsync<TResponse>(IEnumerable<object> requestDtos, CancellationToken token = new CancellationToken())
        {
            return GetGatewayAsync(requestDtos.GetType().GetCollectionType()).SendAllAsync<TResponse>(requestDtos, token);
        }

        public Task PublishAsync(object requestDto, CancellationToken token = new CancellationToken())
        {
            return GetGatewayAsync(requestDto.GetType()).PublishAsync(requestDto, token);
        }

        public Task PublishAllAsync(IEnumerable<object> requestDtos, CancellationToken token = new CancellationToken())
        {
            return GetGatewayAsync(requestDtos.GetType().GetCollectionType()).PublishAllAsync(requestDtos, token);
        }
    }
}