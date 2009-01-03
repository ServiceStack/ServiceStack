using System.Collections.Generic;
using System.Runtime.Serialization;
using Sakila.ServiceModel.Version100.Types;

namespace Sakila.ServiceModel.Version100.Operations.SakilaService
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class GetFilmsResponse : IExtensibleDataObject
	{
		public GetFilmsResponse()
		{
			Version = 100;
			ResponseStatus = new ResponseStatus();
			Films = new List<Film>();
		}

		[DataMember]
		public List<Film> Films { get; set; }


		[DataMember]
		public int Version { get; set; }
		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
		[DataMember]
		public Properties Properties { get; set; }
		public ExtensionDataObject ExtensionData { get; set; }
	}
}