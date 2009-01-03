using System.Collections.Generic;
using System.Runtime.Serialization;
using Sakila.ServiceModel.Version100.Types;

namespace Sakila.ServiceModel.Version100.Operations.SakilaService
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class GetAllCustomersResponse : IExtensibleDataObject
	{
		public GetAllCustomersResponse()
		{
			Version = 100;
			ResponseStatus = new ResponseStatus();
			Customers = new List<Customer>();
		}

		[DataMember]
		public List<Customer> Customers { get; set; }


		[DataMember]
		public int Version { get; set; }
		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
		[DataMember]
		public Properties Properties { get; set; }
		public ExtensionDataObject ExtensionData { get; set; }
	}
}