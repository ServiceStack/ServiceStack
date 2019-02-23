using System;
using System.Threading.Tasks;
using ServiceStack.Messaging;

namespace ServiceStack.Web
{
    public interface IServiceRunner
    {
        object Process(IRequest requestContext, object instance, object request);
    }

    public interface IServiceRunner<TRequest> : IServiceRunner
    {
        void OnBeforeExecute(IRequest req, TRequest request, object service);
        object OnAfterExecute(IRequest req, object response, object service);

        Task<object> HandleExceptionAsync(IRequest request, TRequest requestDto, Exception ex, object instance);

        Task<object> ExecuteAsync(IRequest req, object instance, TRequest requestDto);

        object Execute(IRequest req, object instance, IMessage<TRequest> request);
        object ExecuteOneWay(IRequest requestContext, object instance, TRequest request);
    }
}