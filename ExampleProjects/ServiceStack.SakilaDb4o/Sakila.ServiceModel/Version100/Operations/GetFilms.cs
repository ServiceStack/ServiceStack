using System.Runtime.Serialization;
using Sakila.ServiceModel.Version100.Types;

namespace Sakila.ServiceModel.Version100.Operations
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class GetFilms : IExtensibleDataObject
	{
		public GetFilms()
		{
			FilmIds = new ArrayOfIntId();
			Version = 100;
		}

		[DataMember]
		public ArrayOfIntId FilmIds { get; set; }


		[DataMember]
		public int Version { get; set; }
		[DataMember]
		public Properties Properties { get; set; }
		public ExtensionDataObject ExtensionData { get; set; }
	}
}