using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.DataAccess.Db4oProvider.Tests
{
	[TestFixture]
	public class Db4OQueryableProviderTests : Db4OTestBase
	{

		[Test]
		public void Query_with_exact_match()
		{
			const string firstName = "Bill";
			var customers = provider.Query<Customer>(x => x.FirstName == firstName);
			Assert.That(customers.Count, Is.EqualTo(1));
			Assert.That(customers[0].FirstName, Is.EqualTo(firstName));
		}

		[Test]
		public void Query_with_partial_match()
		{
			const string firstName = "il";
			var customers = provider.Query<Customer>(x => x.FirstName.Contains(firstName));
			Assert.That(customers.Count, Is.EqualTo(1));
			Assert.That(customers[0].FirstName.Contains(firstName), Is.True);
		}
		
	}
}