using System.Runtime.Serialization;

namespace Sakila.ServiceModel.Version100.Types
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class Property
	{
		public string Name { get; set; }
		public string Value { get; set; }
	}
}