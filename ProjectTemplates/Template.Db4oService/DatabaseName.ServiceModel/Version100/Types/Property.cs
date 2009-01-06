using System.Runtime.Serialization;

namespace @ServiceModelNamespace@.Version100.Types
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class Property
	{
		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public string Value { get; set; }
	}
}