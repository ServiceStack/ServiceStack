using System.Runtime.Serialization;
using ServiceStack.Translators;

namespace Sakila.ServiceModel.Version100.Types
{
	[TranslateModel(typeof(DomainModel.Country))]
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public partial class Country 
	{
		[DataMember]
		public int Id { get; set; }
		[DataMember]
		public string Name { get; set; }
	}
}