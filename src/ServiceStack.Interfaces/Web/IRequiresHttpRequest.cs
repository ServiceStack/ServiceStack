namespace ServiceStack.Web
{
	public interface IRequiresHttpRequest
	{
		IHttpRequest HttpRequest { get; set; }
	}
}
