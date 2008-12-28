using System.Runtime.Serialization;

namespace Sakila.ServiceModel.Version100.Types
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class Address 
	{
		public int Id { get; set; }
		public string Line1 { get; set; }
		public string Line2 { get; set; }
		public string Town { get; set; }
		public City City { get; set; }
		public string PostCode { get; set; }
	}
}