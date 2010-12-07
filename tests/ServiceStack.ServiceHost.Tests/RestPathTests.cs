using System;
using NUnit.Framework;

namespace ServiceStack.ServiceHost.Tests
{
	[TestFixture]
	public class RestPathTests
	{
		public class SimpleType
		{
			public string Name { get; set; }
		}

		[Test]
		public void Can_deserialize_SimpleType_path()
		{
			var restPath = new RestPath(typeof(SimpleType), new RestPathAttribute("/simple/{Name}"));
			var request = restPath.CreateRequest("/simple/HelloWorld!") as SimpleType;

			Assert.That(request, Is.Not.Null);
			Assert.That(request.Name, Is.EqualTo("HelloWorld!"));
		}

		[Test]
		public void Can_deserialize_SimpleType_in_middle_of_path()
		{
			var restPath = new RestPath(typeof(SimpleType), new RestPathAttribute("/simple/{Name}/some-other-literal"));
			var request = restPath.CreateRequest("/simple/HelloWorld!/some-other-literal") as SimpleType;

			Assert.That(request, Is.Not.Null);
			Assert.That(request.Name, Is.EqualTo("HelloWorld!"));
		}


		public class ComplexType
		{
			public int Id { get; set; }

			public string Name { get; set; }

			public Guid UniqueId { get; set; }
		}

		[Test]
		public void Can_deserialize_ComplexType_path()
		{

			var restPath = new RestPath(typeof(ComplexType),
				new RestPathAttribute("/Complex/{Id}/{Name}/Unique/{UniqueId}"));
			var request = restPath.CreateRequest(
				"/complex/5/Is Alive/unique/4583B364-BBDC-427F-A289-C2923DEBD547") as ComplexType;

			Assert.That(request, Is.Not.Null);
			Assert.That(request.Id, Is.EqualTo(5));
			Assert.That(request.Name, Is.EqualTo("Is Alive"));
			Assert.That(request.UniqueId, Is.EqualTo(new Guid("4583B364-BBDC-427F-A289-C2923DEBD547")));
		}

		public class BbcMusicRequest
		{
			public Guid mbz_guid { get; set; }

			public string release_type { get; set; }

			public string content_type { get; set; }
		}

		private static void AssertMatch(string definitionPath, string requestPath, BbcMusicRequest expectedRequest)
		{
			var restPath = new RestPath(typeof(BbcMusicRequest), new RestPathAttribute(definitionPath));

			var reqestTestPath = requestPath.ToLower().Split('/');
			Assert.That(restPath.IsMatch(reqestTestPath));

			var actualRequest = restPath.CreateRequest(requestPath) as BbcMusicRequest;

			Assert.That(actualRequest, Is.Not.Null);
			Assert.That(actualRequest.mbz_guid, Is.EqualTo(expectedRequest.mbz_guid));
			Assert.That(actualRequest.release_type, Is.EqualTo(expectedRequest.release_type));
			Assert.That(actualRequest.content_type, Is.EqualTo(expectedRequest.content_type));
		}

		[Test]
		public void Can_support_BBC_Rest_Apis()
		{
			/*
				/music/artists/:mbz_guid.[xml|yaml|json]
				/music/artists/:mbz_guid/promotions.[json]
				/music/artists/:mbz_guid/releases.[xml|yaml|json]
				/music/artists/:mbz_guid/releases/[albums|singles|eps|...].[xml|yaml|json]
			*/
			var mbz = new Guid("E0A387F5-48F0-40E0-AAEA-483DD7EE7484");

			AssertMatch("/music/artists/{mbz_guid}.{content_type}",
				"/music/artists/E0A387F5-48F0-40E0-AAEA-483DD7EE7484.xml",
				new BbcMusicRequest { mbz_guid = mbz, content_type = "xml" });

			AssertMatch("/music/artists/{mbz_guid}/promotions.{content_type}",
				"/music/artists/E0A387F5-48F0-40E0-AAEA-483DD7EE7484/promotions.json",
				new BbcMusicRequest { mbz_guid = mbz, content_type = "xml" });

			AssertMatch("/music/artists/{mbz_guid}/releases.{content_type}",
				"/music/artists/E0A387F5-48F0-40E0-AAEA-483DD7EE7484/releases.yaml",
				new BbcMusicRequest { mbz_guid = mbz, content_type = "yaml" });

			AssertMatch("/music/artists/{mbz_guid}/releases.{content_type}",
				"/music/artists/E0A387F5-48F0-40E0-AAEA-483DD7EE7484/releases/eps.json",
				new BbcMusicRequest { mbz_guid = mbz, content_type = "json" });
		}

	}
}