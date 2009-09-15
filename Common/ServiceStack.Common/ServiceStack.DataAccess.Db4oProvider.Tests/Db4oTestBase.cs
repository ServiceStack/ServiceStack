using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Common.Utils;

namespace ServiceStack.DataAccess.Db4oProvider.Tests
{
	[TestFixture]
	public class Db4OTestBase
	{
		private static readonly string Db4ODatabasePath = @"~\Lib\test.db4o".MapAbsolutePath();

		protected IPersistenceProviderManager db4OProviderManager;
		protected IQueryablePersistenceProvider provider;
		protected Customer[] Customers { get; set; }

		public class PreferredCustomer : Customer
		{
			public PreferredCustomer(){}

			public PreferredCustomer(long id, string firstName, string lastName)
				: base(id, firstName, lastName)
			{}

			public float Discount { get; set; }
		}

		public class Customer
		{
			public Customer()
			{
			}

			public Customer(long id, string firstName, string lastName)
			{
				Id = id;
				FirstName = firstName;
				LastName = lastName;
			}

			public long Id { get; set; }
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

		public class CustomerOrder
		{
			public long Id { get; set; }
		}

		[TestFixtureSetUp]
		public void Init()
		{
			if (File.Exists(Db4ODatabasePath))
			{
				File.Delete(Db4ODatabasePath);
			}
			db4OProviderManager = new Db4OFileProviderManager(Db4ODatabasePath);
			provider = (IQueryablePersistenceProvider)db4OProviderManager.GetProvider();
			CreateTestDatabase();
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			provider.Dispose();
			db4OProviderManager.Dispose();
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
