using System.Runtime.Serialization;
using Sakila.ServiceModel.Version100.Types;

namespace Sakila.ServiceModel.Version100.Operations.SakilaService
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class StoreCustomersResponse : IExtensibleDataObject
	{
		public StoreCustomersResponse()
		{
			Version = 100;
		}

		[DataMember]
		public int Version { get; set; }
		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
		[DataMember]
		public Properties Properties { get; set; }
		public ExtensionDataObject ExtensionData { get; set; }
	}
}