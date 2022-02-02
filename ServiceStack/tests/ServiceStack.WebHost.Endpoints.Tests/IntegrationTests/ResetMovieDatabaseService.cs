namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	public class ResetMovieDatabaseService : IService
	{
		public object Any(ResetMovieDatabase request)
		{
			return new ResetMovieDatabaseResponse();
		}
	}
}