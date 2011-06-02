using System.IO;
using System.Web;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints
{
	public delegate IHttpHandler HttpHandlerResolverDelegate(string httpMethod, string pathInfo, string filePath);
}