using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using ServiceStack.Translators;

namespace Sakila.ServiceModel.Version100.Types
{
	[TranslateModel(typeof(DomainModel.Customer))]
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public partial class Customer 
	{
		[DataMember]
		public int Id { get; set; }
		[DataMember]
		public int StoreId { get; set; }
		[DataMember]
		public string FirstName { get; set; }
		[DataMember]
		public string LastName { get; set; }
		[DataMember]
		public string Email { get; set; }
		[DataMember]
		public Address Address { get; set; }
	}
}