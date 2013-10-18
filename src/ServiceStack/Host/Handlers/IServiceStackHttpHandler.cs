using System.Threading.Tasks;
using ServiceStack.Web;

namespace ServiceStack.Host.Handlers
{
	public interface IServiceStackHttpHandler
	{
        Task ProcessRequestAsync(IHttpRequest httpReq, IHttpResponse httpRes, string operationName);
		void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName);
	}
}