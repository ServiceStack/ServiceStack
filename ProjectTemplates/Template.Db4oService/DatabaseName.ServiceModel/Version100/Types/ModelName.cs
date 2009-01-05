using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace @ServiceModelNamespace@.Version100.Types
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class @ModelName@ 
	{
		[DataMember]
		public int Id { get; set; }
	}
}