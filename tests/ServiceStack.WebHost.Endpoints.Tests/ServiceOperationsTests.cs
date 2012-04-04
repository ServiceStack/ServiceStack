using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using ServiceStack.ServiceHost;
using ServiceStack.WebHost.Endpoints.Tests.Support.Operations;
using ServiceStack.WebHost.Endpoints.Utils;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class ServiceOperationsTests
	{
		private static ServiceOperations CreateServiceOperations(Assembly assembly, string operationNamespace)
		{
			var operationTypes = new List<Type>();
			foreach (var type in assembly.GetTypes())
			{
				if (type.Namespace == null) continue;
				if (type.Namespace.StartsWith(operationNamespace))
				{
					operationTypes.Add(type);
				}
			}
			return new ServiceOperations(operationTypes);
		}

		[Test]
		public void ServiceOperations_only_provides_uniquely_named_types()
		{
			var operations = CreateServiceOperations(typeof(GetCustomers).Assembly, typeof(GetCustomers).Namespace);
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
			var operations = CreateServiceOperations(typeof(GetCustomers).Assembly, typeof(GetCustomers).Namespace);
			var schemaSet = XsdUtils.GetXmlSchemaSet(operations.AllOperations.Types);
			var schemas = schemaSet.Schemas();
			Assert.IsNotNull(schemas);
		}

	}
}