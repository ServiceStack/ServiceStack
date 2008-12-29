using System.Collections.Generic;
using MySql.Data.MySqlClient;
using NHibernate;
using NHibernate.Criterion;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Logging;
using ServiceStack.Sakila.Tests.Integration.Support;
using DataModel = ServiceStack.Sakila.DataAccess.DataModel;

namespace ServiceStack.Sakila.Tests.Integration.DataAccess
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

				var sqlCommand = new MySqlCommand("select * from customer where customer_id = " + this.CustomerId, connection);
				var reader = sqlCommand.ExecuteReader();

				reader.Read();
				log.DebugFormat("Customer Id: {0}, Name: {1} {2}",
					reader.GetInt32("customer_id"), reader.GetString("first_name"), reader.GetString("last_name"));

				Assert.That(reader.GetUInt32("customer_id"), Is.EqualTo(this.CustomerId));

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

			var customers = session.CreateCriteria(typeof (DataModel.Customer))
				.Add(Restrictions.Eq("Id", base.CustomerId)).List();
			
			Assert.That(customers.Count, Is.EqualTo(1));

			var customer = (DataModel.Customer)customers[0];
			Assert.That(customer.Id, Is.EqualTo(base.CustomerId));
		}

		[Test]
		public void Get_customer_with_IPersistenceProvider()
		{
			using (var provider = this.ProviderManager.CreateProvider())
			{
				var customer = provider.GetById<DataModel.Customer>(base.CustomerId);

				Assert.That(customer, Is.Not.Null);
				Assert.That(customer.Id, Is.EqualTo(base.CustomerId));
			}
		}
    }
}