using System.Runtime.Serialization;

namespace Sakila.ServiceModel.Version100.Types
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class Country 
	{
		[DataMember]
		public int Id { get; set; }
		[DataMember]
		public string Name { get; set; }
	}
}