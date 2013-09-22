using ServiceStack.ServiceHost;

namespace ServiceStack.Server
{
	public interface IHttpError : IHttpResult
	{
		string Message { get; }
		string ErrorCode { get; }
	}
}