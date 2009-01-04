using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.DataAccess.Db4oProvider.Tests
{
	[TestFixture]
	public class Db4oAdvancedQueryTests : Db4oTestBase
	{
		public class ArrayOfIntId : List<int>
		{
			public ArrayOfIntId() { }
			public ArrayOfIntId(IEnumerable<int> collection) : base(collection) { }
		}
		
		[Test]
		public void GetByIds_using_ArrayOfIntId()
		{
			var ids = new ArrayOfIntId(new[] {1, 2, 3});
			var customers = provider.GetByIds<Customer>(ids);
			Assert.That(customers.Count, Is.EqualTo(ids.Count));
		}
	}
}