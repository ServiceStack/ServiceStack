using System.Runtime.Serialization;

namespace @ServiceModelNamespace@.Version100.Types
{
	[DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
	public class Property
	{
		public string Name { get; set; }
		public string Value { get; set; }
	}
}