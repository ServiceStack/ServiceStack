using System;
using System.IO;
using Db4objects.Db4o;
using NUnit.Framework;
using Sakila.DomainModel;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Common.Utils;
using ServiceStack.Configuration;
using ServiceStack.DataAccess;
using ServiceStack.DataAccess.Db4oProvider;
using ServiceStack.DataAccess.NHibernateProvider;
using ServiceStack.Sakila.Logic;
using ServiceStack.Sakila.Logic.LogicInterface;
using ServiceStack.ServiceInterface;

namespace ServiceStack.Utils
{
	[TestFixture]
	public class CreateDb4oDatabase
	{
		private const string db4oDatabasePath = @"C:\Projects\code.google\ExampleProjects\ServiceStack.Sakila\ServiceStack.Utils\Lib\sakila.db4o";
		private const string connectionString = @"server=localhost;database=Sakila;uid=root;password=root";
		private readonly string[] xmlAssemblyMapping = { "ServiceStack.Sakila.DataAccess" };


		private IPersistenceProviderManager db4oProviderManager;
		private IPersistenceProvider db4oProvider;
		private ISakilaServiceFacade facade;

		public OperationContext CreateOperationContext()
		{
			var nhFactory = NHibernateProviderManagerFactory.CreateMySqlFactory(xmlAssemblyMapping);
			return new OperationContext {
				Cache = new MemoryCacheClient(),
				Factory = new FactoryProvider(null, nhFactory.CreateProviderManager(connectionString)),
				Resources = new ConfigurationResourceManager(),
			};
		}

		[TestFixtureSetUp]
		public void Init()
		{
			try
			{
				if (File.Exists(db4oDatabasePath))
				{
					File.Delete(db4oDatabasePath);
				}
				db4oProviderManager = new Db4oFileProviderManager(db4oDatabasePath);
				db4oProvider = db4oProviderManager.CreateProvider();
				facade = new SakilaServiceFacade(CreateOperationContext());

			}
			catch (Exception ex)
			{
				throw;
			}
		}

		[Test]
		public void AddAllCustomers()
		{
			try
			{
				var mysqlCustomers = facade.GetAllCustomers();
				foreach (var mysqlCustomer in mysqlCustomers)
				{
					db4oProvider.Store(mysqlCustomer);
				}
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		[Test]
		public void PrintAllCustomers()
		{
			var customers = db4oProvider.GetAll<Customer>();
			foreach (var customer in customers)
			{
				ObjectDumperUtils.Write(customer);
			}
		}

	}
}
