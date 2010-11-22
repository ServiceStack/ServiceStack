using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;
using Sakila.ServiceModel.Version100.Operations.SakilaService;
using Sakila.ServiceModel.Version100.Types;
using ServiceStack.Common.Extensions;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceModel.Extensions;
using ServiceStack.UsageExamples.Support;
using ServiceStack.UsageExamples.Support.Translators;

namespace ServiceStack.UsageExamples
{
	/// <summary>
	/// Examples on accessing the service using the Service Model classes from ProductService.Service.Model.dll assembly.
	/// DB Recommends this approach for accessing internally hosted services. 
	/// Since the Assembly only contains POCO dto classes, there is no risk of it becoming a c# fat client.
	/// 
	/// The <see cref="UsingDtoFromXsd">alternate approach</see> would be to:
	/// use the XSD > To Generate DTO assembly > and use the generated classes instead,
	/// which would require more code, time and effort.
	/// </summary>
	/// 
	[TestFixture]
	public class UsingDtoFromAssembly : TestBase
	{
		/// <summary>
		/// Simple request, using xlinq to extract one field.
		/// </summary>
		[Test]
		public void Get_customers_using_dto_from_assembly_and_parse_with_xlinq()
		{
			using (var client = new Soap12ServiceClient(base.WsSyncReplyUri))
			{
				var request = new GetCustomers { CustomerIds = new ArrayOfIntId { CustomerId } };
				var response = client.Send(request);
				var el = XNode.ReadFrom(response.GetReaderAtBodyContents()) as XElement;
				var customers = el.AnyElement("Customers").AllElements("Customer").ToList();

				Assert.AreEqual(1, customers.Count);
				Assert.AreEqual(CustomerId, customers[0].GetInt("Id"));
			}
		}

		/// <summary>
		/// Simple request, using xlinq to extract one field.
		/// </summary>
		[Test]
		public void Get_customers_using_dto_from_assembly_and_parse_with_xlinq_BasicHttp()
		{
			using (var client = new Soap11ServiceClient(base.BasicHttpSyncReplyUri))
			{
				var request = new GetCustomers { CustomerIds = new ArrayOfIntId { CustomerId } };
				var response = client.Send(request);
				var el = XNode.ReadFrom(response.GetReaderAtBodyContents()) as XElement;
				var customers = el.AnyElement("Customers").AllElements("Customer").ToList();

				Assert.AreEqual(1, customers.Count);
				Assert.AreEqual(CustomerId, customers[0].GetInt("Id"));
			}
		}

		/// <summary>
		/// The DB Recommended way to parse a request. 
		/// Use xlinq to extraxt only the data you need directly into your application model.
		/// </summary>
		[Test]
		public void Get_customers_using_dto_from_assembly_and_parse_all_with_xlinq()
		{
			using (var client = new Soap12ServiceClient(base.WsSyncReplyUri))
			{
				var request = new GetCustomers { CustomerIds = new ArrayOfIntId { CustomerId } };
				var response = client.Send(request);
				var el = XNode.ReadFrom(response.GetReaderAtBodyContents()) as XElement;

				var customers = CustomerTranslator.Instance.ParseAll(el.AnyElement("Customers").AllElements("Customer")).ToList();

				Assert.AreEqual(1, customers.Count);
				Assert.AreEqual(CustomerId, customers[0].Id);
			}
		}

		/// <summary>
		/// Using deserialization to parse request.
		/// 
		/// Note: it is considered bad practice to use the deserialized DTO objects within your application.
		/// Instead you should copy the data from the service dto into your application model.
		/// </summary>
		[Test]
		public void Get_customers_using_dto_from_assembly_and_deserialize()
		{
			using (var client = new Soap12ServiceClient(base.WsSyncReplyUri))
			{
				var request = new GetCustomers { CustomerIds = new ArrayOfIntId { CustomerId } };
				var response = client.Send(request);
				var customersResponse = response.GetBody<GetCustomersResponse>();
				Assert.AreEqual(1, customersResponse.Customers.Count);
				Assert.AreEqual(CustomerId, customersResponse.Customers[0].Id);
			}
		}

	}
}