#if !NETCORE
using System.Web;
#endif
using System;
using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack.Host
{
    public delegate IHttpHandler HttpHandlerResolverDelegate(string httpMethod, string pathInfo, string filePath);

    public delegate bool StreamSerializerResolverDelegate(IRequest requestContext, object dto, IResponse httpRes);

    public delegate void HandleUncaughtExceptionDelegate(
        IRequest httpReq, IResponse httpRes, string operationName, Exception ex);

    public delegate Task HandleUncaughtExceptionAsyncDelegate(
        IRequest httpReq, IResponse httpRes, string operationName, Exception ex);

    public delegate object HandleServiceExceptionDelegate(IRequest httpReq, object request, Exception ex);

    public delegate Task<object> HandleServiceExceptionAsyncDelegate(IRequest httpReq, object request, Exception ex);

    public delegate void HandleGatewayExceptionDelegate(IRequest httpReq, object request, Exception ex);
    public delegate Task HandleGatewayExceptionAsyncDelegate(IRequest httpReq, object request, Exception ex);

    public delegate RestPath FallbackRestPathDelegate(IHttpRequest httpReq);

    public interface ITypedFilter
    {
        void Invoke(IRequest req, IResponse res, object dto);
    }

    public interface ITypedFilterAsync
    {
        Task InvokeAsync(IRequest req, IResponse res, object dto);
    }

    public interface ITypedFilter<in T>
    {
        void Invoke(IRequest req, IResponse res, T dto);
    }

    public interface ITypedFilterAsync<in T>
    {
        Task InvokeAsync(IRequest req, IResponse res, T dto);
    }

    public class TypedFilter<T> : ITypedFilter
    {
        private readonly Action<IRequest, IResponse, T> action;
        public TypedFilter(Action<IRequest, IResponse, T> action)
        {
            this.action = action;
        }

        public void Invoke(IRequest req, IResponse res, object dto)
        {
            action(req, res, (T)dto);
        }
    }

    public class TypedFilterAsync<T> : ITypedFilterAsync
    {
        private readonly Func<IRequest, IResponse, T, Task> action;
        public TypedFilterAsync(Func<IRequest, IResponse, T, Task> action)
        {
            this.action = action;
        }

        public async Task InvokeAsync(IRequest req, IResponse res, object dto)
        {
            await action(req, res, (T)dto);
        }
    }
}