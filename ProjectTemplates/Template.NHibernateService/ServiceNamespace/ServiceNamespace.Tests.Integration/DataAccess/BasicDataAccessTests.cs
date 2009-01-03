using System.Collections.Generic;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Criterion;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Logging;
using @ServiceNamespace@.Tests.Integration.Support;
using DataModel = @ServiceNamespace@.DataAccess.DataModel;

namespace @ServiceNamespace@.Tests.Integration.DataAccess
{
    [TestFixture]
    public class BasicDataAccessTests : IntegrationTestBase
    {
    	private readonly ILog log = LogManager.GetLogger(typeof (BasicDataAccessTests));

        [Test]
		public void Get_customer_with_raw_MySqlConnection()
        {
			using (var connection = new MySqlConnection(this.Config.LocalConnectionString))
			{
				connection.Open();

				var sqlCommand = new MySqlCommand("select * from customer where customer_id = " + this.@ModelName@Id, connection);
				var reader = sqlCommand.ExecuteReader();

				reader.Read();
				log.DebugFormat("@ModelName@ Id: {0}, Name: {1} {2}",
					reader.GetInt32("customer_id"), reader.GetString("first_name"), reader.GetString("last_name"));

				Assert.That(reader.GetUInt32("customer_id"), Is.EqualTo(this.@ModelName@Id));

				reader.Close();
				connection.Close();
			}
        }

		[Test]
		public void Get_customer_with_NHibernate_ISession()
		{
			IDictionary<string, string> properties = new Dictionary<string, string>
			{
				{"connection.provider", "NHibernate.Connection.DriverConnectionProvider"},
				{"dialect", "NHibernate.Dialect.MySQLDialect"},
				{"connection.driver_class", "NHibernate.Driver.MySqlDataDriver"},
				{"connection.connection_string", base.Config.LocalConnectionString},
			};

			var configuration = new NHibernate.Cfg.Configuration().SetProperties(properties);
			configuration.AddAssembly(base.Config.MappingAssemblyName);

			var sessionFactory = configuration.BuildSessionFactory();;
			var session = sessionFactory.OpenSession();

			var customers = session.CreateCriteria(typeof (DataModel.@ModelName@))
				.Add(Restrictions.Eq("Id", base.@ModelName@Id)).List();
			
			Assert.That(customers.Count, Is.EqualTo(1));

			var customer = (DataModel.@ModelName@)customers[0];
			Assert.That(customer.Id, Is.EqualTo(base.@ModelName@Id));
		}

		[Test]
		public void Get_customer_with_IPersistenceProvider()
		{
			using (var provider = this.ProviderManager.CreateProvider())
			{
				var customer = provider.GetById<DataModel.@ModelName@>(base.@ModelName@Id);

				Assert.That(customer, Is.Not.Null);
				Assert.That(customer.Id, Is.EqualTo(base.@ModelName@Id));
			}
		}
    }
}