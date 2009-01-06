using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Sakila.ServiceModel.Version100.Types
{
	[CollectionDataContract(Namespace = "http://schemas.servicestack.net/types/", ItemName = "Id")]
	public class ArrayOfGuidId : List<Guid>
	{
		public ArrayOfGuidId() { }
		public ArrayOfGuidId(IEnumerable<Guid> collection) : base(collection) { }
	}
}