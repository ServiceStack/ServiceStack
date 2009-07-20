using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.Examples.ServiceInterface.Types
{
	[CollectionDataContract(Namespace = "http://schemas.sericestack.net/examples/types", ItemName = "long")]
	public class ArrayOfLong : List<long>
	{
		public ArrayOfLong() { }
		public ArrayOfLong(IEnumerable<long> collection) : base(collection) { }
		public ArrayOfLong(params long[] collection) : base(collection) { }
	}
}