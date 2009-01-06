using System.Linq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using Sakila.DomainModel;

namespace ServiceStack.DataAccess.Db4oProvider.Tests
{
	[TestFixture]
	public class Db4oSakilaDatabaseTests
	{
		private const string db4oDatabasePath = @"C:\Projects\code.google\Common\ServiceStack.Common\ServiceStack.DataAccess.Db4oProvider.Tests\Lib\sakila.db4o";

		protected IPersistenceProviderManager db4oProviderManager;
		protected IPersistenceProvider provider;

		[TestFixtureSetUp]
		public void Init()
		{
			db4oProviderManager = new Db4oFileProviderManager(db4oDatabasePath);
			provider = db4oProviderManager.GetProvider();
		}


		[Test]
		public void Sakila_GetByIntIds()
		{
			var ids = new[] { 1, 2, 3 };
			var customers = provider.GetByIds<Customer>(ids);
			Assert.That(customers.ToList().ConvertAll(x => x.Id), Is.EquivalentTo(ids));
		}

	}
}