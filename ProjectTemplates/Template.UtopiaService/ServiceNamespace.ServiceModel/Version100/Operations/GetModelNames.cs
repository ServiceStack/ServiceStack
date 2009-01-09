using System.Collections.Generic;
using System.Runtime.Serialization;
using @ServiceModelNamespace@.Version100.Types;

namespace @ServiceModelNamespace@.Version100.Operations
{
	[DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
	public class Get@ModelName@s : IExtensibleDataObject
	{
		public Get@ModelName@s()
		{
			this.Version = 100;
		}

		[DataMember]
		public List<long> @ModelName@Ids { get; set; }

		[DataMember]
		public int Version { get; set; }
		[DataMember]
		public Properties Properties { get; set; }
		public ExtensionDataObject ExtensionData { get; set; }
	}
}