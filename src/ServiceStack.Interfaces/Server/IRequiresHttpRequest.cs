namespace ServiceStack.Server
{
	public interface IRequiresHttpRequest
	{
		IHttpRequest HttpRequest { get; set; }
	}
}
