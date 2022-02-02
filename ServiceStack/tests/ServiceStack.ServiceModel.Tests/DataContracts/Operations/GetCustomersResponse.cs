using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.ServiceModel.Tests.DataContracts.Operations
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class GetCustomersResponse : IExtensibleDataObject
	{
		public GetCustomersResponse()
		{
			this.Version = 100;
			this.Customers = new List<Customer>();
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