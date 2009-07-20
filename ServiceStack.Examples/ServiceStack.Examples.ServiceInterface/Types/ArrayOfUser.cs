using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.Examples.ServiceInterface.Types
{
	[CollectionDataContract(Namespace = "http://schemas.sericestack.net/examples/types", ItemName = "User")]
	public class ArrayOfUser : List<User>
	{
		public ArrayOfUser() { }
		public ArrayOfUser(IEnumerable<User> collection) : base(collection) { }
	}
}