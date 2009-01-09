using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace @ServiceModelNamespace@.Version100.Types
{
	[CollectionDataContract(Namespace = "http://schemas.ddnglobal.com/types/", ItemName = "Id")]
	public class ArrayOfGuidId : List<Guid>
	{
		public ArrayOfGuidId() { }
		public ArrayOfGuidId(IEnumerable<Guid> collection) : base(collection) { }
	}
}