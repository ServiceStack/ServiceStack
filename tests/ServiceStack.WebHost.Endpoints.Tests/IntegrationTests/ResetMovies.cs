using System.ComponentModel;
using System.Runtime.Serialization;
using ServiceStack.OrmLite;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	[DataContract]
	[Description("Resets the database back to the original Top 5 movies.")]
	[Route("/reset-movies")]
	public class ResetMovies { }

	[DataContract]
	public class ResetMoviesResponse : IHasResponseStatus
	{
		public ResetMoviesResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class ResetMoviesService : ServiceInterface.Service
	{
		public object OnPost(ResetMovies request)
		{
			ConfigureDatabase.Init(TryResolve<IDbConnectionFactory>());

			return new ResetMoviesResponse();
		}
	}
}