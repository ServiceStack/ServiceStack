namespace ServiceStack.Web.Handlers
{
	public interface IServiceStackHttpHandler
	{
		void ProcessRequest(IHttpRequest httpReq, IHttpResponse httpRes, string operationName);
	}
}