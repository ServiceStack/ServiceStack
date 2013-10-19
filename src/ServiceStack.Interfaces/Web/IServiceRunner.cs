using System;
using ServiceStack.Messaging;

namespace ServiceStack.Web
{
    public interface IServiceRunner
    {
        object Process(IRequest requestContext, object instance, object request);
    }

    public interface IServiceRunner<TRequest> : IServiceRunner
    {
        void OnBeforeExecute(IRequest requestContext, TRequest request);
        object OnAfterExecute(IRequest requestContext, object response);
        object HandleException(IRequest request, TRequest requestDto, Exception ex);

        object Execute(IRequest requestContext, object instance, TRequest requestDto);
        object Execute(IRequest requestContext, object instance, IMessage<TRequest> request);
        object ExecuteOneWay(IRequest requestContext, object instance, TRequest request);
    }
}