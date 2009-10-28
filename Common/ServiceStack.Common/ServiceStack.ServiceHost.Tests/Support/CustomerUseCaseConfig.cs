namespace ServiceStack.ServiceHost.Tests.Support
{
	public class CustomerUseCaseConfig
	{
		public CustomerUseCaseConfig()
		{
			this.UseCache = true;
		}

		public bool UseCache { get; set; }
	}
}