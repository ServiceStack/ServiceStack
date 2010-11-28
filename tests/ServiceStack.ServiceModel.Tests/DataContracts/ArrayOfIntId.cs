using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.ServiceModel.Tests.DataContracts
{
	[CollectionDataContract(Namespace = "http://schemas.servicestack.net/types/", ItemName = "Id")]
	public class ArrayOfIntId : List<int>
	{
		public ArrayOfIntId() { }
		public ArrayOfIntId(IEnumerable<int> collection) : base(collection) { }
	}
}