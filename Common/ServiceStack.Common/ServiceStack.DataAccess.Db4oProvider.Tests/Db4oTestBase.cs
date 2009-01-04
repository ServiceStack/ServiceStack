using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;

namespace ServiceStack.DataAccess.Db4oProvider.Tests
{
	[TestFixture]
	public class Db4oTestBase
	{
		private const string db4oDatabasePath = @"C:\Projects\code.google\Common\ServiceStack.Common\ServiceStack.DataAccess.Db4oProvider.Tests\Lib\test.db4o";

		protected IPersistenceProviderManager db4oProviderManager;
		protected IPersistenceProvider provider;
		protected Customer[] Customers { get; set; }

		public class PreferredCustomer : Customer
		{
			public PreferredCustomer(int id, string firstName, string lastName) : base(id, firstName, lastName)
			{}

			public float Discount { get; set; }
		}

		public class Customer
		{
			public Customer(int id, string firstName, string lastName)
			{
				Id = id;
				FirstName = firstName;
				LastName = lastName;
			}

			public int Id { get; set; }
			public string FirstName { get; set; }
			public string LastName { get; set; }

			public string Name
			{
				get
				{
					return string.Format("{0} {1}", this.FirstName, this.LastName);
				}
			}
		}

		[TestFixtureSetUp]
		public void Init()
		{
			if (File.Exists(db4oDatabasePath))
			{
				File.Delete(db4oDatabasePath);
			}
			db4oProviderManager = new Db4oFileProviderManager(db4oDatabasePath);
			provider = db4oProviderManager.CreateProvider();
			CreateTestDatabase();
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			provider.Dispose();
			db4oProviderManager.Dispose();
		}

		public void CreateTestDatabase()
		{
			Customers = new[] {
				new Customer(1, "Steve", "Jobs"),
				new Customer(2, "Sergey", "Brin"),
				new Customer(3, "Larry", "Page"),
				new Customer(4, "Eric", "Schmidt"),
				new Customer(5, "Bill", "Gates"),
				new Customer(6, "Bram", "Cohen"),
				new Customer(7, "Mike", "Morhaime"),
				new Customer(8, "Jimmy", "Wales"),
				new Customer(9, "Ray", "Ozzie"),
				new Customer(10, "Jeff", "Bezos"),
				new Customer(11, "Steve", "Ballmer"),
				new Customer(12, "Steve", "Wozniak"),
				new PreferredCustomer(13, "Preferred", "Customer"),
			};
			foreach (var customer in Customers)
			{
				provider.Store(customer);
			}
		}
	}
}
