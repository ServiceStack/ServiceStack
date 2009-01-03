using System.Collections.Generic;
using System.Runtime.Serialization;

namespace @ServiceModelNamespace@.Version100.Types
{
	[CollectionDataContract(Namespace = "http://schemas.servicestack.net/types/", ItemName = "Id")]
	public class ArrayOfIntId : List<int>
	{
		public ArrayOfIntId() { }
		public ArrayOfIntId(IEnumerable<int> collection) : base(collection) { }
	}
}