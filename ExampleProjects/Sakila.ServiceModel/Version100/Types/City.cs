using System.Runtime.Serialization;

namespace Sakila.ServiceModel.Version100.Types
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class City 
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public Country Country { get; set; }
	}
}