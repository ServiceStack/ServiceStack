using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.Examples.ServiceInterface;

namespace ServiceStack.Examples.ServiceModel.Types
{
	[CollectionDataContract(Namespace = ExampleConfig.DefaultNamespace, ItemName = "User")]
	public class ArrayOfUser : List<User>
	{
		public ArrayOfUser() { }
		public ArrayOfUser(IEnumerable<User> collection) : base(collection) { }
	}
}