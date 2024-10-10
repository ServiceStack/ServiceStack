using System.Runtime.Serialization;
#if NETFRAMEWORK
using ServiceStack.ServiceModel;
#endif

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class ResetMovieDatabase
	{
	}

	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class ResetMovieDatabaseResponse
	{
		public ResetMovieDatabaseResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}