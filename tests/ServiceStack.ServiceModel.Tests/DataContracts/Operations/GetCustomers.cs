using System.Runtime.Serialization;

namespace ServiceStack.ServiceModel.Tests.DataContracts.Operations
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class GetCustomers : IExtensibleDataObject
	{
		public GetCustomers()
		{
			this.CustomerIds = new ArrayOfIntId();
			this.Version = 100;
		}

		[DataMember]
		public ArrayOfIntId CustomerIds { get; set; }


		[DataMember]
		public int Version { get; set; }
		[DataMember]
		public Properties Properties { get; set; }
		public ExtensionDataObject ExtensionData { get; set; }
	}
}