using System.Runtime.Serialization;
using @ServiceModelNamespace@.Version100.Types;

namespace @ServiceModelNamespace@.Version100.Operations.@ServiceName@
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class GetAll@ModelName@s : IExtensibleDataObject
	{
		public GetAll@ModelName@s()
		{
			Version = 100;
		}

		[DataMember]
		public int Version { get; set; }
		[DataMember]
		public Properties Properties { get; set; }
		public ExtensionDataObject ExtensionData { get; set; }
	}
}