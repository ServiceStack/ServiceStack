using System.Runtime.Serialization;
using ServiceStack.Translators;

namespace Sakila.ServiceModel.Version100.Types
{
	[TranslateModel(typeof(DomainModel.Address))]
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public partial class Address 
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