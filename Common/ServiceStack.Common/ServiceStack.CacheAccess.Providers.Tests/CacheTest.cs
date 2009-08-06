using System;
using System.Runtime.Serialization;

namespace ServiceStack.CacheAccess.Providers.Tests
{
	[Serializable]
	[DataContract]
	public class CacheTest
	{
		public CacheTest(int value)
		{
			this.Value = value;
		}

		[DataMember]
		public int Value { get; set; }
	}
}