using System.Runtime.Serialization;
using ServiceStack.Translators;

namespace @ServiceModelNamespace@.Version100.Types
{
	[TranslateModel(typeof(DomainModel.@ModelName@))]
	[DataContract(Namespace = "http://schemas.ddnglobal.com/types/")]
	public partial class @ModelName@
	{
		[DataMember]
		public long @ModelName@Id { get; set; }
	}
}