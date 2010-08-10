using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.ServiceInterface.ServiceModel;
using Movie=ServiceStack.Examples.ServiceModel.Types.Movie;

namespace ServiceStack.Examples.ServiceModel.Operations
{
	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class Movies
	{
		[DataMember(EmitDefaultValue = false)]
		public string Id { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public Movie Movie { get; set; }
	}

	[DataContract(Namespace = ExampleConfig.DefaultNamespace)]
	public class MoviesResponse
	{
		public MoviesResponse()
		{
			this.ResponseStatus = new ResponseStatus();
			this.Movies = new List<Movie>();
		}

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }

		[DataMember(EmitDefaultValue = false)]
		public List<Movie> Movies { get; set; }
	}
}