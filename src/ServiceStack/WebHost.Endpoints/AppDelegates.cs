using System;
using System.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints
{
	public delegate IHttpHandler HttpHandlerResolverDelegate(string httpMethod, string pathInfo, string filePath);

	public delegate bool StreamSerializerResolverDelegate(IRequestContext requestContext, object dto, IHttpResponse httpRes);

    public delegate void HandleUncaughtExceptionDelegate(
        IHttpRequest httpReq, IHttpResponse httpRes, string operationName, Exception ex);

    public delegate object HandleServiceExceptionDelegate(IHttpRequest httpReq, object request, Exception ex);

    public delegate RestPath FallbackRestPathDelegate(string httpMethod, string pathInfo, string filePath);
}