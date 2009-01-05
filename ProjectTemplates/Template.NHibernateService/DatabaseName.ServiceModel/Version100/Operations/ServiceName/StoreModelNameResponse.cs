using System.Runtime.Serialization;
using @ServiceModelNamespace@.Version100.Types;

namespace @ServiceModelNamespace@.Version100.Operations.@ServiceName@
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class Store@ModelName@Response : IExtensibleDataObject
	{
		public Store@ModelName@Response()
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