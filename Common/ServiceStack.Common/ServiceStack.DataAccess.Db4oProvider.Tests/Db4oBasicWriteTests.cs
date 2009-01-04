using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.DataAccess.Db4oProvider.Tests
{
	[TestFixture]
	public class Db4oBasicWriteTests : Db4oTestBase
	{

		[Test]
		public void Add_Update_and_Store()
		{
			var newCustomer = new Customer(100, "New", "Customer");
			provider.Store(newCustomer);

			var storedCustomer = provider.GetById<Customer>(100);
			Assert.That(storedCustomer.Name, Is.EqualTo(newCustomer.Name));

			storedCustomer.FirstName = "Modified";
			provider.Store(storedCustomer);

			var modifiedCustomer = provider.GetById<Customer>(100);
			Assert.That(modifiedCustomer.FirstName, Is.EqualTo("Modified"));

			provider.Delete(modifiedCustomer);
			var deletedCustomer = provider.GetById<Customer>(100);
			Assert.That(deletedCustomer, Is.Null);
		}
		
	}
}