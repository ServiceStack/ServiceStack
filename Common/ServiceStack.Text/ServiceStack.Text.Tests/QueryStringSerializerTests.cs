using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ServiceStack.Text.Tests
{
	public class C
	{
		public int? A { get; set; }
		public int? B { get; set; }
	}


	[TestFixture]
	public class QueryStringSerializerTests
	{
		[Test]
		public void Can_serialize_query_string()
		{
			Assert.That(QueryStringSerializer.SerializeToString(new C { A = 1, B = 2 }),
				Is.EqualTo("A=1&B=2"));

			Assert.That(QueryStringSerializer.SerializeToString(new C { A = null, B = 2 }),
				Is.EqualTo("B=2"));
		}
	}
}
