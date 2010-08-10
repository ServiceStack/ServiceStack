using System.Runtime.Serialization;
using ServiceStack.Examples.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.Examples.ServiceModel.Operations
{
	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class PopulateMovies
	{
	}

	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class PopulateMoviesResponse
	{
		public PopulateMoviesResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}
}