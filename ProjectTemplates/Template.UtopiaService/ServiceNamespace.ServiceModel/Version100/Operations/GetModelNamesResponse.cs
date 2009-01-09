using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using @ServiceModelNamespace@.Version100.Types;

namespace @ServiceModelNamespace@.Version100.Operations
{
	[DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
	public class Get@ModelName@sResponse : IExtensibleDataObject
	{
		public Get@ModelName@sResponse()
		{
			this.Version = 100;
		}

		[DataMember]
		public List<@ModelName@> @ModelName@s { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }

		[DataMember]
		public int Version { get; set; }

		[DataMember]
		public Properties Properties { get; set; }

		public ExtensionDataObject ExtensionData { get; set; }
	}
}