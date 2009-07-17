using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.Model.Version100
{

	[CollectionDataContract(Namespace = "http://schemas.servicestack.net/types", ItemName = "String")]
	public class ArrayOfString : List<string>
	{
		public ArrayOfString()
		{
		}

		public ArrayOfString(IEnumerable<string> collection) : base(collection) { }
		public ArrayOfString(params string[] args) : base(args) { }
	}

	[CollectionDataContract(Namespace = "http://schemas.servicestack.net/types", ItemName = "Id")]
	public class ArrayOfStringId : List<string>
	{
		public ArrayOfStringId()
		{
		}

		public ArrayOfStringId(IEnumerable<string> collection) : base(collection) { }
		public ArrayOfStringId(params string[] args) : base(args) { }
	}


	[CollectionDataContract(Namespace = "http://schemas.servicestack.net/types", ItemName = "Guid")]
	public class ArrayOfGuid : List<Guid>
	{
		public ArrayOfGuid()
		{
		}

		public ArrayOfGuid(IEnumerable<Guid> collection) : base(collection) { }
		public ArrayOfGuid(params Guid[] args) : base(args) { }
	}

	[CollectionDataContract(Namespace = "http://schemas.servicestack.net/types", ItemName = "Id")]
	public class ArrayOfGuidId : List<Guid>
	{
		public ArrayOfGuidId()
		{
		}

		public ArrayOfGuidId(IEnumerable<Guid> collection) : base(collection) { }
		public ArrayOfGuidId(params Guid[] args) : base(args) { }
	}

}
