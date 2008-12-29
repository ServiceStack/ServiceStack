using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Sakila.ServiceModel.Version100.Types
{
	[CollectionDataContract(Namespace = "http://schemas.servicestack.net/types/", ItemName = "Id")]
	public class ArrayOfStringId : List<string>
	{
		public ArrayOfStringId() { }
		public ArrayOfStringId(IEnumerable<string> collection) : base(collection) { }
	}
}