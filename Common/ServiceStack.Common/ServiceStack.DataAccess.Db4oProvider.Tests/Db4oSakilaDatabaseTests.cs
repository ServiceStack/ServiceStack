using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Sakila.DomainModel;
using ServiceStack.Common.Utils;

namespace ServiceStack.DataAccess.Db4oProvider.Tests
{
	[TestFixture]
	public class Db4OSakilaDatabaseTests
	{
		private static readonly string Db4ODatabasePath = @"~\Lib\test.db4o".MapAbsolutePath();

		protected IPersistenceProviderManager db4OProviderManager;
		protected IPersistenceProvider provider;

		[TestFixtureSetUp]
		public void Init()
		{
			db4OProviderManager = new Db4OFileProviderManager(Db4ODatabasePath);
			provider = db4OProviderManager.GetProvider();
		}


		[Test]
		public void Sakila_GetByIntIds()
		{
			var ids = new[] { 1, 2, 3 };
			provider.Store(new Customer { Id = 1 });
			provider.Store(new Customer { Id = 2 });
			provider.Store(new Customer { Id = 3 });

			var customers = provider.GetByIds<Customer>(ids);
			Assert.That(customers.ToList().ConvertAll(x => x.Id), Is.EquivalentTo(ids));
		}

	}
}