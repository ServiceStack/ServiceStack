using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.ServiceModel.Tests.DataContracts
{
	[CollectionDataContract(Namespace = "http://schemas.servicestack.net/types/", ItemName = "Property")]
	public class Properties : List<Property>
	{
		public Properties() { }
		public Properties(IEnumerable<Property> collection) : base(collection) { }
	}
}