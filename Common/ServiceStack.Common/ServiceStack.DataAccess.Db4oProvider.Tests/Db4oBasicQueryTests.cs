using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using System.Linq;

namespace ServiceStack.DataAccess.Db4oProvider.Tests
{
	[TestFixture]
	public class Db4oBasicQueryTests : Db4oTestBase
	{

		[Test]
		public void GetAll()
		{
			var customers = provider.GetAll<Customer>();
			Assert.That(customers.Count, Is.EqualTo(this.Customers.Length));
		}

		[Test]
		public void GetAllOrderedBy_LastName_Asc()
		{
			var orderedCustomerLastNames = Customers.OrderBy(x => x.LastName).Select(x => x.LastName).ToList();
			var customers = provider.GetAllOrderedBy<Customer>("LastName", true).Select(x => x.LastName).ToList();
			for (var i=0; i < orderedCustomerLastNames.Count; i++)
			{
				Assert.That(customers[i], Is.EqualTo(orderedCustomerLastNames[i]));
			}
		}

		[Test]
		public void GetByIntId()
		{
			var customer = provider.GetById<Customer>(5);
			Assert.That(customer.Name, Is.EqualTo("Bill Gates"));
		}

		[Test]
		public void GetByIntId_from_super_type_without_id_field()
		{
			var preferredCustomer = provider.GetById<PreferredCustomer>(13);
			Assert.That(preferredCustomer.Id, Is.EqualTo(13));
		}

		[Test]
		public void GetByIntIds()
		{
			var googleFounderIds = new[] { 2, 3 };
			var customers = provider.GetByIds<Customer>(googleFounderIds);
			Assert.That(customers.ToList().ConvertAll(x => x.Id), Is.EquivalentTo(googleFounderIds));
		}

		[Test]
		public void FindByValue()
		{
			var customer = provider.FindByValue<Customer>("FirstName", "Bill");
			Assert.That(customer.Name, Is.EqualTo("Bill Gates"));
		}

		[Test]
		public void FindAllByValue()
		{
			var allSteveIds = this.Customers.Where(x => x.FirstName == "Steve").Select(x => x.Id).ToList();
			var customers = provider.FindAllByValue<Customer>("FirstName", "Steve");
			Assert.That(customers.Select(x => x.Id).ToList(), Is.EquivalentTo(allSteveIds));
		}

		[Test]
		public void FindByValues()
		{
			var googleFounderIds = new[] { 2, 3 };
			var googleFoundersFirstNames = Customers.Where(x => googleFounderIds.Contains(x.Id)).Select(x => x.FirstName).ToList();
			var customers = provider.FindByValues<Customer>("FirstName", googleFoundersFirstNames);
			Assert.That(customers.Select(x => x.Id).ToList(), Is.EquivalentTo(googleFounderIds));
		}

	}
}