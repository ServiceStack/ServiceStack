using System.Runtime.Serialization;
using @ServiceModelNamespace@.Version100.Types;

namespace @ServiceModelNamespace@.Version100.Operations.@ServiceName@
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class Get@ModelName@s : IExtensibleDataObject
	{
		public Get@ModelName@s()
		{
			@ModelName@Ids = new ArrayOfIntId();
			Version = 100;
		}

		[DataMember]
		public ArrayOfIntId @ModelName@Ids { get; set; }


		[DataMember]
		public int Version { get; set; }
		[DataMember]
		public Properties Properties { get; set; }
		public ExtensionDataObject ExtensionData { get; set; }
	}
}