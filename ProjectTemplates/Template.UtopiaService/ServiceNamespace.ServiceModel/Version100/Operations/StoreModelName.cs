using System.Runtime.Serialization;
using @ServiceModelNamespace@.Version100.Types;

namespace @ServiceModelNamespace@.Version100.Operations
{
	[DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
	public class Store@ModelName@ : IExtensibleDataObject
	{
		public Store@ModelName@()
		{
			this.Version = 100;
		}

		[DataMember]
		public @ModelName@ @ModelName@ { get; set; }

		[DataMember]
		public int Version { get; set; }

		[DataMember]
		public Properties Properties { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}
}