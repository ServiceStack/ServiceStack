namespace ServiceStack.WebHost.Endpoints
{
	public class ServiceHostEnvironment
	{
		public WebServerType WebServer { get; set; }

		public string HostName { get; set; }

		public string HttpHandlerPath { get; set; }

		public string BaseUrl { get; set; }

		public string BasePath { get; set; }
	}
}