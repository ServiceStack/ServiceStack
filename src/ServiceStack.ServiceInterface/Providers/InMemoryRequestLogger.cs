using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface.Providers
{
	public class RequestLog
	{
		public string IpAddress { get; set; }
		public string HttpMethod { get; set; }
		public string[] Headers { get; set; }
		public object RequestDto { get; set; }
	}

	public class InMemoryRequestLogger : IRequestLogger
	{
		public void Log(IRequestContext requestContext, object requestDto)
		{
			var httpReq = requestContext.Get<IHttpRequest>();
			var entry = new RequestLog {
				HttpMethod = httpReq.HttpMethod,
				RequestDto = requestDto,
			};
		}
	}
}