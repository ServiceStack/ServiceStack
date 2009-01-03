using System.Runtime.Serialization;

namespace Sakila.ServiceModel.Version100.Types
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class Actor
	{
		[DataMember]
		public string FirstName { get; set; }
		[DataMember]
		public string LastName { get; set; }
	}
}