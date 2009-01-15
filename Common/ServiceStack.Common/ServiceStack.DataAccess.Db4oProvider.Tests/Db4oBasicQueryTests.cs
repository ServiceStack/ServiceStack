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
		public void GetByLongId()
		{
			var customer = provider.GetById<Customer>((long)5);
			Assert.That(customer.Name, Is.EqualTo("Bill Gates"));
		}

		[Test]
		public void GetByIntId_from_super_type_without_id_field()
		{
			var preferredCustomer = provider.GetById<PreferredCustomer>(13);
			Assert.That(preferredCustomer.Id, Is.EqualTo(13));
		}

		[Test]
		public void Longs_and_ints_with_the_same_value_are_equal()
		{
			int intVal = 5;
			long longVal = intVal;
			Assert.That(longVal == intVal, Is.True);
			Assert.That(longVal.Equals(intVal), Is.True);
			Assert.That(intVal == longVal, Is.True);
			Assert.That(intVal.Equals(longVal), Is.False);

			object oIntVal = intVal;
			object oLongVal = longVal;
			Assert.That(oLongVal == oIntVal, Is.False);
			Assert.That(oLongVal.Equals(oIntVal), Is.False);
			Assert.That(oIntVal.Equals(oLongVal), Is.False);
		}

		[Test]
		public void Testing_int_and_long_values_in_a_collection()
		{
			int intVal = 5;
			long longVal = 5;
			long[] longValues = new long[] { 3, 4, 5 };
			int[] intValues = new int[] { 3, 4, 5 };

			Assert.That(longValues.Contains(intVal), Is.True);
			Assert.That(intValues.Contains((int)longVal), Is.True);
		}

		[Test]
		public void GetByIntIds()
		{
			var googleFounderIds = new[] { 2, 3 };
			var customers = provider.GetByIds<Customer>(googleFounderIds);
			Assert.That(customers.ToList().ConvertAll(x => x.Id), Is.EquivalentTo(googleFounderIds.ToList().ConvertAll(x => (long)x)));
		}

		[Test]
		public void GetByLongIds()
		{
			var googleFounderIds = new long[] { 2, 3 };
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
			var googleFounderIds = new long[] { 2, 3 };
			var googleFoundersFirstNames = Customers.Where(x => googleFounderIds.Contains(x.Id)).Select(x => x.FirstName).ToList();
			var customers = provider.FindByValues<Customer>("FirstName", googleFoundersFirstNames);
			Assert.That(customers.Select(x => x.Id).ToList(), Is.EquivalentTo(googleFounderIds));
		}

	}
}