namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[Route("/returnsvoid")]
	public class ReturnsVoid : IReturnVoid
	{
		public string Name { get; set; }
	}

    public class ReturnsVoidService : IService
	{
        public void Any(ReturnsVoid request) {}
	}

}