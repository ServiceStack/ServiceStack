using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.Common.Extensions;

namespace RedisWebServices.ServiceModel.Types
{
	[CollectionDataContract(ItemName = "KeyValuePair")]
	public class ArrayOfKeyValuePair 
		: List<KeyValuePair>
	{
		public ArrayOfKeyValuePair() { }
		public ArrayOfKeyValuePair(IEnumerable<KeyValuePair> collection) : base(collection) { }
		public ArrayOfKeyValuePair(IEnumerable<KeyValuePair<string, string>> collection) 
			: base(collection.ConvertAll(x => new KeyValuePair(x.Key, x.Value))) { }
	}
}