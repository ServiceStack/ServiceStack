using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.ServiceHost.Tests;

[TestFixture]
public class RestPathTests
{
    private ServiceStackHost appHost;

    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        appHost = new BasicAppHost().Init();
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown()
    {
        appHost.Dispose();
    }

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
        var config = HostContext.Config;

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
        
        public bool? Active { get; set; }
        
        public int? Age { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        
        public double? Weight { get; set; }
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

    public class ComplexTypeWithFields(int id, string name, Guid uniqueId)
    {
        public readonly int Id = id;

        public readonly string Name = name;

        public readonly Guid UniqueId = uniqueId;
    }

    [Test]
    public void Can_deserialize_ComplexTypeWithFields_path()
    {
        using (JsConfig.With(new Config { IncludePublicFields = true }))
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

        var requestTestPath = RestPath.GetPathPartsForMatching(requestPath);
        Assert.That(restPath.IsMatch("GET", requestTestPath, out _), Is.True);

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
        Assert.That(restPath.IsMatch("GET", reqestTestPath, out _), Is.True);

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
            new RackSpaceRequest { });
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
        var restPath = new RestPath(typeof(SlugRequest), definitionPath);
        var requestTestPath = RestPath.GetPathPartsForMatching(requestPath);
        Assert.That(restPath.IsMatch("GET", requestTestPath, out _), Is.True);

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
        var restPath = new RestPath(typeof(SlugRequest), definitionPath);
        var requestTestPath = RestPath.GetPathPartsForMatching(requestPath);
        Assert.That(restPath.IsMatch("GET", requestTestPath, out _), Is.False);
    }

    [Test]
    public void Cannot_have_variable_after_wildcard()
    {
        Assert.Throws<ArgumentException>(() => {
            AssertMatch("/content/{Slug*}/{Version}",
                "/content/wildcard/slug/path/1", "*/content", new SlugRequest(), -1);
        });
    }

    [Test]
    public void Can_support_internal_wildcard()
    {
        AssertMatch("/content/{Slug*}/literal",
            "/content/wildcard/slug/path/literal",
            "*/content",
            new SlugRequest { Slug = "wildcard/slug/path" },
            97901);

        AssertMatch("/content/{Slug*}/version/{Version}",
            "/content/wildcard/slug/path/version/1",
            "*/content",
            new SlugRequest { Slug = "wildcard/slug/path", Version = 1 },
            97801);

        AssertMatch("/content/{Slug*}/with/{Options*}",
            "/content/wildcard/slug/path/with/optionA/optionB",
            "*/content",
            new SlugRequest { Slug = "wildcard/slug/path", Options = "optionA/optionB" },
            95801);

        AssertMatch("/{Slug*}/content", "/content", "*/content", new SlugRequest(), 100901);

        AssertMatch("/content/{Slug*}/literal", "/content/literal", "*/content", new SlugRequest(), 100901);

        AssertNoMatch("/content/{Slug*}/literal", "/content/wildcard/slug/path");

        AssertNoMatch("/content/{Slug*}/literal", "/content/literal/literal");
    }

    [Test]
    public void Routes_have_expected_precedence()
    {
        AssertPrecedence("GET /content",
            "GET /content",
            "ANY /content",
            "GET /content/{Slug*}",
            "ANY /content/{Slug*}");

        AssertPrecedence("PUT /content",
            "PUT /content",
            "ANY /content",
            "PUT /content/{Slug*}",
            "ANY /content/{Slug*}");

        AssertPrecedence("GET /content/literal",
            "GET /content/literal",
            "ANY /content/literal",
            "GET /content/{Version}",
            "ANY /content/{Version}",
            "GET /content/{Slug*}",
            "ANY /content/{Slug*}");

        AssertPrecedence("PUT /content/literal",
            "PUT /content/literal",
            "ANY /content/literal",
            "PUT /content/{Version}",
            "ANY /content/{Version}",
            "PUT /content/{Slug*}",
            "ANY /content/{Slug*}");

        AssertPrecedence("GET /content/v1",
            "GET /content/{Version}",
            "ANY /content/{Version}",
            "GET /content/{Slug*}",
            "ANY /content/{Slug*}");

        AssertPrecedence("PUT /content/v1",
            "PUT /content/{Version}",
            "ANY /content/{Version}",
            "PUT /content/{Slug*}",
            "ANY /content/{Slug*}");

        AssertPrecedence("GET /content/v1/literal-after",
            "GET /content/v1/literal-after",
            "ANY /content/v1/literal-after",
            "GET /content/{Version}/literal-after",
            "ANY /content/{Version}/literal-after",
            "GET /content/{Version}/{Slug}",
            "ANY /content/{Version}/{Slug}",
            "GET /content/{Slug*}",
            "ANY /content/{Slug*}");

        AssertPrecedence("PUT /content/v1/literal-after",
            "ANY /content/v1/literal-after",
            "ANY /content/{Version}/literal-after",
            "ANY /content/{Version}/{Slug}",
            "PUT /content/{Slug*}",
            "ANY /content/{Slug*}");

        AssertPrecedence("GET /content/literal-before/v1",
            "GET /content/literal-before/v1",
            "ANY /content/literal-before/v1",
            "GET /content/literal-before/{Version}",
            "ANY /content/literal-before/{Version}",
            "GET /content/{Version}/{Slug}",
            "ANY /content/{Version}/{Slug}",
            "GET /content/{Slug*}",
            "ANY /content/{Slug*}");

        AssertPrecedence("PUT /content/literal-before/v1",
            "ANY /content/literal-before/v1",
            "ANY /content/literal-before/{Version}",
            "ANY /content/{Version}/{Slug}",
            "PUT /content/{Slug*}",
            "ANY /content/{Slug*}");

        AssertPrecedence("GET /content/v1/literal/slug",
            "GET /content/v1/literal/slug",
            "ANY /content/v1/literal/slug",
            "GET /content/v1/literal/{ignore}",
            "GET /content/{ignore}/literal/{ignore}",
            "GET /content/{Version*}/literal/{Slug*}",
            "ANY /content/{Version*}/literal/{Slug*}",
            "GET /content/{Slug*}",
            "ANY /content/{Slug*}");

        AssertPrecedence("PUT /content/v1/literal/slug",
            "ANY /content/v1/literal/slug",
            "ANY /content/{Version*}/literal/{Slug*}",
            "PUT /content/{Slug*}",
            "ANY /content/{Slug*}");
    }

    class SlugRoute
    {
        public static SlugRoute[] Definitions =
        [
            new SlugRoute("GET /content"),
            new SlugRoute("PUT /content"),
            new SlugRoute("ANY /content"),

            new SlugRoute("GET /content/literal"),
            new SlugRoute("PUT /content/literal"),
            new SlugRoute("ANY /content/literal"),

            new SlugRoute("GET /content/{Version}"),
            new SlugRoute("PUT /content/{Version}"),
            new SlugRoute("ANY /content/{Version}"),

            new SlugRoute("GET /content/{Slug*}"),
            new SlugRoute("PUT /content/{Slug*}"),
            new SlugRoute("ANY /content/{Slug*}"),

            new SlugRoute("GET /content/v1/literal-after"),
            new SlugRoute("ANY /content/v1/literal-after"),
            new SlugRoute("GET /content/{Version}/literal-after"),
            new SlugRoute("ANY /content/{Version}/literal-after"),
            new SlugRoute("GET /content/{Version}/{Slug}"),
            new SlugRoute("ANY /content/{Version}/{Slug}"),

            new SlugRoute("GET /content/literal-before/v1"),
            new SlugRoute("ANY /content/literal-before/v1"),
            new SlugRoute("GET /content/literal-before/{Version}"),
            new SlugRoute("ANY /content/literal-before/{Version}"),

            new SlugRoute("GET /content/v1/literal/slug"),
            new SlugRoute("ANY /content/v1/literal/slug"),
            new SlugRoute("GET /content/v1/literal/{ignore}"),
            new SlugRoute("GET /content/{ignore}/literal/{ignore}"),
            new SlugRoute("GET /content/{Version*}/literal/{Slug*}"),
            new SlugRoute("ANY /content/{Version*}/literal/{Slug*}")

        ];

        public string Definition { get; set; }
        public RestPath RestPath { get; set; }
        public int Score { get; set; }

        public SlugRoute(string definition)
        {
            this.Definition = definition;
            var parts = definition.SplitOnFirst(' ');
            RestPath = new RestPath(typeof(SlugRequest), path: parts[1], verbs: parts[0] == ActionContext.AnyAction ? null : parts[0]);
        }

        public static List<SlugRoute> GetOrderedMatchingRules(string withVerb, string forPath)
        {
            var matchingRoutes = new List<SlugRoute>();

            foreach (var definition in Definitions)
            {
                var pathComponents = RestPath.GetPathPartsForMatching(forPath);
                definition.Score = definition.RestPath.MatchScore(withVerb, pathComponents);
                if (definition.Score > 0)
                {
                    matchingRoutes.Add(definition);
                }
            }

            var orderedRoutes = matchingRoutes.OrderByDescending(x => x.Score).ToList();
            return orderedRoutes;
        }
    }

    public void AssertPrecedence(string requestedDefinition, params string[] expected)
    {
        var parts = requestedDefinition.SplitOnFirst(' ');
        var orderedRoutes = SlugRoute.GetOrderedMatchingRules(parts[0], parts[1]);
        var matchingDefinitions = orderedRoutes.ConvertAll(x => x.Definition);

        var isMatch = matchingDefinitions.EquivalentTo(expected);

        Assert.That(isMatch, "Expected:\n{0}\n  Actual:\n{1}".Fmt(expected.Join("\n"), matchingDefinitions.Join("\n")));
    }

    [Test]
    public void Can_match_lowercase_http_method()
    {
        var restPath = new RestPath(typeof(ComplexType), "/Complex/{Id}/{Name}/Unique/{UniqueId}", "PUT");
        var withPathInfoParts = RestPath.GetPathPartsForMatching("/complex/5/Is Alive/unique/4583B364-BBDC-427F-A289-C2923DEBD547");
        Assert.That(restPath.IsMatch("put", withPathInfoParts, out _));
    }

    [Test]
    public void Can_parse_endpoint_routing_syntax()
    {
        void AssertArguments(string path, string[] expectedArgs, string[] expectedConstraints)
        {
            var restPath = new RestPath(typeof(ComplexType), path);
            Assert.That(restPath.VariablesNames, Is.EquivalentTo(expectedArgs));
            Assert.That(restPath.Constraints, Is.EquivalentTo(expectedConstraints));
        }

        // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/routing
        AssertArguments("/path", [null], [null]);
        AssertArguments("/path/{Id}", [null, "Id"], [null, null]);
        AssertArguments("/path/{Id:int}", [null, "Id"], [null, "int"]);
        AssertArguments("/path/{Id?}", [null, "Id"], [null, null]);
        AssertArguments("/path/{*Name}", [null, "Name"], [null, null]);
        AssertArguments("/path/{**Name}", [null, "Name"], [null, null]);
        AssertArguments("/path/{Active:bool}", [null, "Active"], [null, "bool"]);
        AssertArguments("/path/{DateOfBirth:datetime}", [null, "DateOfBirth"], [null, "datetime"]);
        AssertArguments("/path/{Weight:double}", [null, "Weight"], [null, "double"]);
        AssertArguments("/path/{UniqueId:guid}", [null, "UniqueId"], [null, "guid"]);
        AssertArguments("/path/{Name:minlength(4)}", [null, "Name"], [null, "minlength(4)"]);
        AssertArguments("/path/{Name:maxlength(8)}", [null, "Name"], [null, "maxlength(8)"]);
        AssertArguments("/path/{Name:length(12)}", [null, "Name"], [null, "length(12)"]);
        AssertArguments("/path/{Name:length(8,16)}", [null, "Name"], [null, "length(8,16)"]);
        AssertArguments("/path/{Age:min(18)}", [null, "Age"], [null, "min(18)"]);
        AssertArguments("/path/{Age:max(120)}", [null, "Age"], [null, "max(120)"]);
        AssertArguments("/path/{Name:alpha}", [null, "Name"], [null, "alpha"]);
        AssertArguments(@"/path/{Name:regex(^\\d{{3}}-\\d{{2}}-\\d{{4}}$)}", [null, "Name"], [null, @"regex(^\\d{{3}}-\\d{{2}}-\\d{{4}}$)"]);
        AssertArguments("/path/{Name:required}", [null, "Name"], [null, "required"]);
    }
}