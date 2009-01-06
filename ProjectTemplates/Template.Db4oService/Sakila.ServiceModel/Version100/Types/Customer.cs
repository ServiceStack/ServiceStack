using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Sakila.ServiceModel.Version100.Types
{
	[DataContract(Namespace = "http://schemas.servicestack.net/types/")]
	public class Customer 
	{
		[DataMember]
		public int Id { get; set; }
	}
}