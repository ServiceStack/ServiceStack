using System.Runtime.Serialization;

namespace Sakila.ServiceModel.Version100.Types
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class Address 
	{
		[DataMember]
		public int Id { get; set; }
		[DataMember]
		public string Line1 { get; set; }
		[DataMember]
		public string Line2 { get; set; }
		[DataMember]
		public string Town { get; set; }
		[DataMember]
		public City City { get; set; }
		[DataMember]
		public string PostCode { get; set; }
	}
}