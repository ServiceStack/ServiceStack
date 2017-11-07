using System.Linq;
using NUnit.Framework;
using ServiceStack.DataAnnotations;
using ServiceStack.Host;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Services;
using ServiceStack.WebHost.Endpoints.Tests.Support.Types;

namespace ServiceStack.WebHost.Endpoints.Tests
{

	[TestFixture]
	public class RouteInferenceTests
	{
        ServiceStackHost appHost;

		[OneTimeSetUp]
		public void InferRoutes()
		{
            appHost = new BasicAppHost().Init();
            
            RouteNamingConvention.PropertyNamesToMatch.Add("Key");
			RouteNamingConvention.AttributeNamesToMatch.Add(typeof(KeyAttribute).Name);
            appHost.Routes.AddFromAssembly(typeof(RouteInferenceTests).Assembly);
		}

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            appHost.Dispose();
        }

		[Test]
		public void Should_infer_route_from_RequestDTO_type()
		{
            var restPath = (from r in appHost.RestPaths
							 where r.RequestType == typeof(RequestNoMembers)
							 select r).FirstOrDefault();

			Assert.That(restPath, Is.Not.Null);

			Assert.That(restPath.PathComponentsCount == 1);

			Assert.That(restPath.AllowedVerbs.Contains("GET"));
			Assert.That(restPath.AllowedVerbs.Contains("POST"));
			Assert.That(restPath.AllowedVerbs.Contains("PUT"));
			Assert.That(restPath.AllowedVerbs.Contains("DELETE"));
			Assert.That(restPath.AllowedVerbs.Contains("PATCH"));
			Assert.That(restPath.AllowedVerbs.Contains("HEAD"));
			Assert.That(restPath.AllowedVerbs.Contains("OPTIONS"));

			Assert.IsTrue(typeof(RequestNoMembers).Name.EqualsIgnoreCase(restPath.Path.Remove(0,1)));
		}

		[Test]
		public void Should_infer_route_from_AnyPublicProperty_named_Id()
		{
            var restPath = (from r in appHost.RestPaths
							where r.RequestType == typeof(RequestWithMemberCalledId)
							//routes without {placeholders} are tested above
							&& r.PathComponentsCount > 1 
							select r).FirstOrDefault();

			Assert.That(restPath, Is.Not.Null);

			Assert.That(restPath.PathComponentsCount == 2);
			Assert.IsTrue(restPath.Path.EndsWithInvariant("{Id}"));
		}

		[Test]
		public void Should_infer_route_from_AnyPublicProperty_named_Ids()
		{
            var restPath = (from r in appHost.RestPaths
							where r.RequestType == typeof(RequestWithMemberCalledIds)
							//routes without {placeholders} are tested above
							&& r.PathComponentsCount > 1
							select r).FirstOrDefault();

			Assert.That(restPath, Is.Not.Null);

			Assert.That(restPath.PathComponentsCount == 2);
			Assert.IsTrue(restPath.Path.EndsWithInvariant("{Ids}"));
		}

		[Test]
		public void Should_infer_route_from_AnyPublicProperty_in_MatchingNameStrategy()
		{
            var restPath = (from r in appHost.RestPaths
							where r.RequestType == typeof(RequestWithMemberCalledSpecialName)
							//routes without {placeholders} are tested above
							&& r.PathComponentsCount > 1
							select r).FirstOrDefault();

			Assert.That(restPath, Is.Not.Null);

			Assert.That(restPath.PathComponentsCount == 2);
			Assert.IsTrue(restPath.Path.EndsWithInvariant("{Key}"));
		}

		[Test]
		public void Should_infer_route_from_AnyPublicProperty_with_PrimaryKeyAttribute()
		{
            var restPath = (from r in appHost.RestPaths
							where r.RequestType == typeof(RequestWithPrimaryKeyAttribute)
							//routes without {placeholders} are tested above
							&& r.PathComponentsCount > 1
							select r).FirstOrDefault();

			Assert.That(restPath, Is.Not.Null);

			Assert.That(restPath.PathComponentsCount == 2);
			//it doesn't matter what the placeholder name is; only that 1 placeholer is in the path
			Assert.IsTrue(restPath.Path.Count(c => c == '}') == 1);
		}

		[Test]
		public void Should_infer_route_from_AnyPublicProperty_in_MatchingAttributeStrategy()
		{
            var restPath = (from r in appHost.RestPaths
							where r.RequestType == typeof(RequestWithMemberWithKeyAttribute)
							//routes without {placeholders} are tested above
							&& r.PathComponentsCount > 1
							select r).FirstOrDefault();

			Assert.That(restPath, Is.Not.Null);

			Assert.That(restPath.PathComponentsCount == 2);
			//it doesn't matter what the placeholder name is; only that 1 placeholer is in the path
			Assert.IsTrue(restPath.Path.Count(c => c == '}') == 1);
		}

		[Test]
		public void Should_infer_route_from_AnyPubicProperty_FromAnyStrategy_AndCompositeTheRoute()
		{
            var restPath = (from r in appHost.RestPaths
							where r.RequestType == typeof(RequestWithCompositeKeys)
							//routes without {placeholders} are tested above
							&& r.PathComponentsCount > 1
							select r).FirstOrDefault();

			Assert.That(restPath, Is.Not.Null);

			Assert.That(restPath.PathComponentsCount == 3);
			//it doesn't matter what the placeholder name is; only that 1 placeholer is in the path
			Assert.IsTrue(restPath.Path.Count(c => c == '}') == 2);
		}
	}

	public class RequestNoMembers { }

	public class RequestWithMemberCalledId
	{
		public object Id { get; set; }
	}

	public class RequestWithMemberCalledIds
	{
		public object[] Ids { get; set; }
	}

	public class RequestWithMemberCalledSpecialName
	{
		public string Key { get; set; }
	}

	public class RequestWithPrimaryKeyAttribute
	{
		[PrimaryKey]
		public int PrimaryKeyAttributeProperty { get; set; }
	}	

	public class RequestWithMemberWithKeyAttribute
	{
		[Key]
		public string KeyAttributeProperty { get; set; }
	}

	public class RequestWithCompositeKeys
	{
		public int Id { get; set; }

		public int Key { get; set; }
	}

	public class RequestNoMembersService : TestRestService<RequestNoMembers> { }
	
	public class RequestWithMemberCalledIdService : TestRestService<RequestWithMemberCalledId> { }
	
	public class RequestWithMemberCalledIdsService : TestRestService<RequestWithMemberCalledIds> { }
	
	public class RequestWithMemberCalledSpecialNameService : TestRestService<RequestWithMemberCalledSpecialName> { }
	
	public class RequestWithPrimaryKeyAttributeService : TestRestService<RequestWithPrimaryKeyAttribute> { }
	
	public class RequestWithMemberWithKeyAttributeService : TestRestService<RequestWithMemberWithKeyAttribute> { }
	
	public class RequestWithCompositeKeysService : TestRestService<RequestWithCompositeKeys> { }
}
