using System.IO;
using System.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints
{
	public delegate IHttpHandler HttpHandlerResolverDelegate(string httpMethod, string pathInfo, string filePath);

	public delegate bool StreamSerializerResolverDelegate(IRequestContext requestContext, object dto, IHttpResponse httpRes);
}