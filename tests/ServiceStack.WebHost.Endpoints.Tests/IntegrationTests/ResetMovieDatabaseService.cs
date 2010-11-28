using System;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	public class ResetMovieDatabaseService
		: IService<ResetMovieDatabase>
	{
		public object Execute(ResetMovieDatabase request)
		{
			return new ResetMovieDatabaseResponse();
		}
	}
}