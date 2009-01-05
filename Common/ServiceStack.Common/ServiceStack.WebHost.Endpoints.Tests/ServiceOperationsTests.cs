using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sakila.ServiceModel.Version100.Operations.SakilaDb4oService;
using ServiceStack.WebHost.Endpoints.Metadata;
using ServiceStack.WebHost.Endpoints.Utils;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class ServiceOperationsTests
	{
		[Test]
		public void ServiceOperations_only_provides_uniquely_named_types()
		{
			var operations = new ServiceOperations(typeof(GetCustomers).Assembly, typeof(GetCustomers).Namespace);
			var uniqueTypeNames = new List<string>();
			foreach (var type in operations.AllOperations.Types)
			{
				Assert.That(!uniqueTypeNames.Contains(type.Name));
				uniqueTypeNames.Add(type.Name);
			}
		}

		[Test]
		public void Can_load_ServiceModel_schemas()
		{
			var operations = new ServiceOperations(typeof(GetCustomers).Assembly, typeof(GetCustomers).Namespace);
			var schemaSet = XsdUtils.GetXmlSchemaSet(operations.AllOperations.Types);
			var schemas = schemaSet.Schemas();
		}
	}
}