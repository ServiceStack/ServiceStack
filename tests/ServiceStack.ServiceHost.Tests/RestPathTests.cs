using System;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;

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
			var restPath = new RestPath(typeof(SimpleType), "/simple/{Name}");
			var request = restPath.CreateRequest("/simple/HelloWorld!") as SimpleType;

			Assert.That(request, Is.Not.Null);
			Assert.That(request.Name, Is.EqualTo("HelloWorld!"));
		}

		[Test]
		public void Can_deserialize_SimpleType_in_middle_of_path()
		{
			var restPath = new RestPath(typeof(SimpleType), "/simple/{Name}/some-other-literal");
			var request = restPath.CreateRequest("/simple/HelloWorld!/some-other-literal") as SimpleType;

			Assert.That(request, Is.Not.Null);
			Assert.That(request.Name, Is.EqualTo("HelloWorld!"));
		}

		[Test]
		public void ShowAllow()
		{
			var config = EndpointHostConfig.Instance;

			const string fileName = "/path/to/image.GIF";
			var fileExt = fileName.Substring(fileName.LastIndexOf('.') + 1);
			Console.WriteLine(fileExt);
			Assert.That(config.AllowFileExtensions.Contains(fileExt));
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
				"/Complex/{Id}/{Name}/Unique/{UniqueId}");
			var request = restPath.CreateRequest(
				"/complex/5/Is Alive/unique/4583B364-BBDC-427F-A289-C2923DEBD547") as ComplexType;

			Assert.That(request, Is.Not.Null);
			Assert.That(request.Id, Is.EqualTo(5));
			Assert.That(request.Name, Is.EqualTo("Is Alive"));
			Assert.That(request.UniqueId, Is.EqualTo(new Guid("4583B364-BBDC-427F-A289-C2923DEBD547")));
		}

		public class ComplexTypeWithFields
		{
			public readonly int Id;

			public readonly string Name;

			public readonly Guid UniqueId;

			public ComplexTypeWithFields(int id, string name, Guid uniqueId)
			{
				Id = id;
				Name = name;
				UniqueId = uniqueId;
			}
		}

		[Test]
		public void Can_deserialize_ComplexTypeWithFields_path()
		{
            using (JsConfig.With(includePublicFields:true))
            {
                var restPath = new RestPath(typeof(ComplexTypeWithFields),
                    "/Complex/{Id}/{Name}/Unique/{UniqueId}");
                var request = restPath.CreateRequest(
                    "/complex/5/Is Alive/unique/4583B364-BBDC-427F-A289-C2923DEBD547") as ComplexTypeWithFields;

                Assert.That(request, Is.Not.Null);
                Assert.That(request.Id, Is.EqualTo(5));
                Assert.That(request.Name, Is.EqualTo("Is Alive"));
                Assert.That(request.UniqueId, Is.EqualTo(new Guid("4583B364-BBDC-427F-A289-C2923DEBD547")));
            }
		}


		public class BbcMusicRequest
		{
			public Guid mbz_guid { get; set; }

			public string release_type { get; set; }

			public string content_type { get; set; }
		}

		private static void AssertMatch(string definitionPath, string requestPath,
			string firstMatchHashKey, BbcMusicRequest expectedRequest)
		{
			var restPath = new RestPath(typeof(BbcMusicRequest), definitionPath);

			var reqestTestPath = RestPath.GetPathPartsForMatching(requestPath);
			Assert.That(restPath.IsMatch("GET", reqestTestPath), Is.True);

			Assert.That(firstMatchHashKey, Is.EqualTo(restPath.FirstMatchHashKey));

			var actualRequest = restPath.CreateRequest(requestPath) as BbcMusicRequest;

			Assert.That(actualRequest, Is.Not.Null);
			Assert.That(actualRequest.mbz_guid, Is.EqualTo(expectedRequest.mbz_guid));
			Assert.That(actualRequest.release_type, Is.EqualTo(expectedRequest.release_type));
			Assert.That(actualRequest.content_type, Is.EqualTo(expectedRequest.content_type));
		}

		[Test]
		public void Can_support_BBC_REST_Apis()
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
				"3/music",
				new BbcMusicRequest { mbz_guid = mbz, content_type = "xml" });

			AssertMatch("/music/artists/{mbz_guid}/promotions.{content_type}",
				"/music/artists/E0A387F5-48F0-40E0-AAEA-483DD7EE7484/promotions.json",
				"4/music",
				new BbcMusicRequest { mbz_guid = mbz, content_type = "json" });

			AssertMatch("/music/artists/{mbz_guid}/releases.{content_type}",
				"/music/artists/E0A387F5-48F0-40E0-AAEA-483DD7EE7484/releases.yaml",
                "4/music",
				new BbcMusicRequest { mbz_guid = mbz, content_type = "yaml" });

			AssertMatch("/music/artists/{mbz_guid}/releases/{release_type}.{content_type}",
				"/music/artists/E0A387F5-48F0-40E0-AAEA-483DD7EE7484/releases/albums.json",
                "5/music",
				new BbcMusicRequest { mbz_guid = mbz, release_type = "albums", content_type = "json" });
		}

		public class RackSpaceRequest
		{
			public string version { get; set; }

			public string id { get; set; }

			public string resource_type { get; set; }

			public string action { get; set; }

			public string content_type { get; set; }
		}


		private static void AssertMatch(string definitionPath, string requestPath,
			string firstMatchHashKey, RackSpaceRequest expectedRequest)
		{
			var restPath = new RestPath(typeof(RackSpaceRequest), definitionPath);

			var reqestTestPath = RestPath.GetPathPartsForMatching(requestPath);
			Assert.That(restPath.IsMatch("GET", reqestTestPath), Is.True);

			Assert.That(firstMatchHashKey, Is.EqualTo(restPath.FirstMatchHashKey));

			var actualRequest = restPath.CreateRequest(requestPath) as RackSpaceRequest;

			Assert.That(actualRequest, Is.Not.Null);
			Assert.That(actualRequest.version, Is.EqualTo(expectedRequest.version));
			Assert.That(actualRequest.id, Is.EqualTo(expectedRequest.id));
			Assert.That(actualRequest.resource_type, Is.EqualTo(expectedRequest.resource_type));
			Assert.That(actualRequest.action, Is.EqualTo(expectedRequest.action));
		}

		[Test]
		public void Can_support_Rackspace_REST_Apis()
		{
			/*
			 * /v1.0/214412/images
			 * /v1.0/214412/images.xml
			 * /servers/id/action
			 * /images/detail 
			 */

			AssertMatch("/{version}/{id}/images", "/v1.0/214412/images", 
				"3/images",
				new RackSpaceRequest { version = "v1.0", id = "214412" });

			AssertMatch("/{version}/{id}/images.{content_type}", "/v1.0/214412/images.xml",
				"3/images",
				new RackSpaceRequest { version = "v1.0", id = "214412", content_type = "xml" });

			AssertMatch("/servers/{id}/{action}", "/servers/214412/delete",
                "3/servers",
				new RackSpaceRequest { id = "214412", action = "delete" });

			AssertMatch("/images/{action}", "/images/detail", 
				"2/images",
				new RackSpaceRequest { action = "detail" });

			AssertMatch("/images/detail", "/images/detail",
				"2/images",
				new RackSpaceRequest{});
		}

        public class SlugRequest
        {
            public string Slug { get; set; }
            public int Version { get; set; }
            public string Options { get; set; }
        }

        private static void AssertMatch(string definitionPath, string requestPath, string firstMatchHashKey,
                                        SlugRequest expectedRequest, int expectedScore)
        {
            var restPath = new RestPath(typeof (SlugRequest), definitionPath);
            var requestTestPath = RestPath.GetPathPartsForMatching(requestPath);
            Assert.That(restPath.IsMatch("GET", requestTestPath), Is.True);

            Assert.That(firstMatchHashKey, Is.EqualTo(restPath.FirstMatchHashKey));

            var actualRequest = restPath.CreateRequest(requestPath) as SlugRequest;
            Assert.That(actualRequest, Is.Not.Null);
            Assert.That(actualRequest.Slug, Is.EqualTo(expectedRequest.Slug));
            Assert.That(actualRequest.Version, Is.EqualTo(expectedRequest.Version));
            Assert.That(actualRequest.Options, Is.EqualTo(expectedRequest.Options));
            Assert.That(restPath.MatchScore("GET", requestTestPath), Is.EqualTo(expectedScore));
        }

        private static void AssertNoMatch(string definitionPath, string requestPath)
        {
            var restPath = new RestPath(typeof (SlugRequest), definitionPath);
            var requestTestPath = RestPath.GetPathPartsForMatching(requestPath);
            Assert.That(restPath.IsMatch("GET", requestTestPath), Is.False);
        }

        [Test]
        [ExpectedException(typeof (ArgumentException))]
        public void Cannot_have_variable_after_wildcard()
        {
            AssertMatch("/content/{Slug*}/{Version}",
                "/content/wildcard/slug/path/1", "*/content", new SlugRequest(), -1);
        }

	    [Test]
	    public void Can_support_internal_wildcard()
	    {
            AssertMatch("/content/{Slug*}/literal",
                        "/content/wildcard/slug/path/literal",
                        "*/content",
                        new SlugRequest { Slug = "wildcard/slug/path" },
                        701);

            AssertMatch("/content/{Slug*}/version/{Version}",
                        "/content/wildcard/slug/path/version/1",
                        "*/content",
                        new SlugRequest { Slug = "wildcard/slug/path", Version = 1 },
                        601);

            AssertMatch("/content/{Slug*}/with/{Options*}",
                        "/content/wildcard/slug/path/with/optionA/optionB",
                        "*/content",
                        new SlugRequest { Slug = "wildcard/slug/path", Options = "optionA/optionB" },
                        501);

            AssertMatch("/{Slug*}/content", "/content", "*/content", new SlugRequest(), 1001);

	        AssertMatch("/content/{Slug*}/literal", "/content/literal", "*/content", new SlugRequest(), 1001);

            AssertNoMatch("/content/{Slug*}/literal", "/content/wildcard/slug/path");

            AssertNoMatch("/content/{Slug*}/literal", "/content/literal/literal");
	        
	    }

        
	}
}