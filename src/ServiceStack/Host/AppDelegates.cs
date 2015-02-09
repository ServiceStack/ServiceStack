using System;
using System.Web;
using ServiceStack.Web;

namespace ServiceStack.Host
{
	public delegate IHttpHandler HttpHandlerResolverDelegate(string httpMethod, string pathInfo, string filePath);

	public delegate bool StreamSerializerResolverDelegate(IRequest requestContext, object dto, IResponse httpRes);

    public delegate void HandleUncaughtExceptionDelegate(
        IRequest httpReq, IResponse httpRes, string operationName, Exception ex);

    public delegate object HandleServiceExceptionDelegate(IRequest httpReq, object request, Exception ex);

    public delegate RestPath FallbackRestPathDelegate(string httpMethod, string pathInfo, string filePath);

    public interface ITypedFilter
    {
        void Invoke(IRequest req, IResponse res, object dto);
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
}