using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.Examples.ServiceModel.Operations
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