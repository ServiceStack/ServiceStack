using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.Examples.ServiceInterface.Types
{
	[CollectionDataContract(ItemName = "string")]
	public class ArrayOfString : List<string>
	{
		public ArrayOfString() { }
		public ArrayOfString(IEnumerable<string> collection) : base(collection) { }
		public ArrayOfString(params string[] collection) : base(collection) { }
	}
}