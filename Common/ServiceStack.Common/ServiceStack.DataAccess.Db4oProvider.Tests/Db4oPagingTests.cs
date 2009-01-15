using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using System.Linq;
using ServiceStack.DataAccess.Criteria;

namespace ServiceStack.DataAccess.Db4oProvider.Tests
{
	[TestFixture]
	public class Db4oPagingTests : Db4oTestBase
	{

		[Test]
		public void Paging_GetAll_starting_at_0_limit_2()
		{
			var customers = provider.GetAll<Customer>(new PagingCriteria(0, 2));
			Assert.That(customers.Count, Is.EqualTo(2));
		}
	}
}