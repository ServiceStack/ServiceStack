using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Logging;
using ServiceStack.MsgPack;
using ServiceStack.Text;

namespace RazorRockstars.Web.Tests
{
    public static class SeedData
    {
        public static Reqstar[] Reqstars = new[] {
            new Reqstar(1, "Foo", "Bar", 20),
            new Reqstar(2, "Something", "Else", 30),
            new Reqstar(3, "Foo2", "Bar2", 20),
        };
    }

    [Route("/reqstars/search", "GET")]
    [Route("/reqstars/aged/{Age}")]
    public class SearchReqstars : IReturn<ReqstarsResponse>
    {
        public int? Age { get; set; }
    }

    [Route("/reqstars", "GET")]
    public class AllReqstars : IReturn<List<Reqstar>> { }

    public class ReqstarsResponse
    {
        public int Total { get; set; }
        public int? Aged { get; set; }
        public List<Reqstar> Results { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/reqstars/reset")]
    public class ResetReqstar : IReturnVoid { }

    [Route("/viewstate")]
    public class ViewState { }
    public class ViewStateResponse { }

    [Route("/reqstars/{Id}", "GET")]
    public class GetReqstar : IReturn<Reqstar>
    {
        public int Id { get; set; }
    }

    [Route("/reqstars/{Id}/delete")]
    public class DeleteReqstar : IReturnVoid
    {
        public int Id { get; set; }
    }

    [Route("/reqstars")]
    public class Reqstar : IReturn<List<Reqstar>>
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Age { get; set; }

        public Reqstar() { }
        public Reqstar(int id, string firstName, string lastName, int age)
        {
            Id = id;
            FirstName = firstName;
            LastName = lastName;
            Age = age;
        }
    }

    [Route("/reqstars/{Id}", "PATCH")]
    public class UpdateReqstar : IReturn<Reqstar>
    {
        public int Id { get; set; }
        public int Age { get; set; }

        public UpdateReqstar() { }
        public UpdateReqstar(int id, int age)
        {
            Id = id;
            Age = age;
        }
    }

    public class RoutelessReqstar : IReturn<RoutelessReqstar>
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    [Route("/throw")]
    [Route("/throw/{StatusCode}")]
    [Route("/throw/{StatusCode}/{Message}")]
    public class Throw
    {
        public int? StatusCode { get; set; }
        public string Message { get; set; }
    }

    public class ThrowResponse
    {
        public string Result { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Api("Service Description")]
    [Route("/annotated/{Name}", "GET", Summary = @"GET Summary", Notes = "GET Notes")]
    [Route("/annotated/{Name}", "POST", Summary = @"POST Summary", Notes = "POST Notes")]
    public class Annotated
    {
        [ApiMember(Name = "Name", Description = "Name Description",
                   ParameterType = "path", DataType = "string", IsRequired = true)]
        public string Name { get; set; }
    }

    [Route("/headers/{Text}")]
    public class Headers : IReturn<HttpWebResponse>
    {
        public string Text { get; set; }
    }

    [Route("/strings/{Text}")]
    public class Strings : IReturn<string>
    {
        public string Text { get; set; }
    }

    [Route("/bytes/{Text}")]
    public class Bytes : IReturn<byte[]>
    {
        public string Text { get; set; }
    }

    [Route("/streams/{Text}")]
    public class Streams : IReturn<Stream>
    {
        public string Text { get; set; }
    }

    [TestFixture]
    public class ReqStarsServiceTests
    {
        public const string Host = "http://localhost:1338";
        private const string BaseUri = Host + "/";

        //private AppHost appHost;

        private Stopwatch startedAt;

        [OneTimeSetUp]
        public void TestFixtureSetUp()
        {
            LogManager.LogFactory = new ConsoleLogFactory();
            startedAt = Stopwatch.StartNew();
        }

        //private IDbConnection db;

        [SetUp]
        public void SetUp()
        {
            var client = new JsonServiceClient(BaseUri);
            client.Get(new ResetReqstar());
        }

        [TearDown]
        public void TearDown()
        {
            //db.Dispose();
        }

        [OneTimeTearDown]
        public void TestFixtureTearDown()
        {
            "Time Taken {0}ms".Fmt(startedAt.ElapsedMilliseconds).Print();
            //appHost.Dispose();
        }

        protected static IRestClient[] RestClients =
        {
            new JsonServiceClient(BaseUri),
            new XmlServiceClient(BaseUri),
            new JsvServiceClient(BaseUri),
            new MsgPackServiceClient(BaseUri),
        };

        protected static IServiceClient[] ServiceClients =
            RestClients.OfType<IServiceClient>().ToArray();


        [Test]
        public void Can_Process_OPTIONS_request_with_Cors_ActionFilter()
        {
            var webReq = WebRequest.CreateHttp(Host + "/reqstars");
            webReq.Method = "OPTIONS";
            using (var r = webReq.GetResponse())
            {
                Assert.That(r.Headers[HttpHeaders.AllowOrigin], Is.EqualTo(CorsFeature.DefaultOrigin));
                Assert.That(r.Headers[HttpHeaders.AllowMethods], Is.EqualTo(CorsFeature.DefaultMethods));
                Assert.That(r.Headers[HttpHeaders.AllowHeaders], Is.EqualTo(CorsFeature.DefaultHeaders));

                var response = r.GetResponseStream().ReadFully();
                Assert.That(response.Length, Is.EqualTo(0));
            }
        }


        [Test, TestCaseSource("RestClients")]
        public void Can_GET_AllReqstars(IRestClient client)
        {
            var allReqstars = client.Get<List<Reqstar>>("/reqstars");
            Assert.That(allReqstars.Count, Is.EqualTo(SeedData.Reqstars.Length));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Can_SEND_AllReqstars_PrettyTypedApi(IServiceClient client)
        {
            var allReqstars = client.Send(new AllReqstars());
            Assert.That(allReqstars.Count, Is.EqualTo(SeedData.Reqstars.Length));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_GET_AllReqstars_PrettyRestApi(IRestClient client)
        {
            var request = new AllReqstars();
            var response = client.Get(request);

            Assert.That(request.ToUrl("GET"), Is.EqualTo("/reqstars"));
            Assert.That(response.Count, Is.EqualTo(SeedData.Reqstars.Length));
        }

        [Test]
        public void Can_GET_AllReqstars_View()
        {
            var html = "{0}/reqstars".Fmt(Host).GetStringFromUrl(accept: "text/html");
            html.Print();
            Assert.That(html, Does.Contain("<!--view:AllReqstars.cshtml-->"));
            Assert.That(html, Does.Contain("<!--template:HtmlReport.cshtml-->"));
        }


        [Test, TestCaseSource("RestClients")]
        public void Can_GET_SearchReqstars(IRestClient client)
        {
            var response = client.Get<ReqstarsResponse>("/reqstars/search");
            Assert.That(response.Results.Count, Is.EqualTo(SeedData.Reqstars.Length));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Disallows_SEND_SearchReqstars_PrettyTypedApi(IServiceClient client)
        {
            try
            {
                var response = client.Send(new SearchReqstars());
                Assert.Fail("POST's to SearchReqstars should not be allowed");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(405));
                Assert.That(webEx.StatusDescription, Is.EqualTo("Method Not Allowed"));
            }
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_GET_SearchReqstars_PrettyRestApi(IRestClient client)
        {
            var request = new SearchReqstars();
            var response = client.Get(request);

            Assert.That(request.ToUrl("GET"), Is.EqualTo("/reqstars/search"));
            Assert.That(response.Results.Count, Is.EqualTo(SeedData.Reqstars.Length));
        }

        [Test, TestCaseSource("RestClients")]
        public void Invalid_GET_SearchReqstars_throws_typed_Response_PrettyRestApi(IRestClient client)
        {
            try
            {
                var response = client.Get(new SearchReqstars { Age = -1 });
                Assert.Fail("POST's to SearchReqstars should not be allowed");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(400));
                Assert.That(webEx.StatusDescription, Is.EqualTo("ArgumentException"));

                Assert.That(webEx.ResponseStatus.ErrorCode, Is.EqualTo("ArgumentException"));
                Assert.That(webEx.ResponseStatus.Message, Is.EqualTo("Invalid Age"));

                Assert.That(webEx.ResponseDto as ReqstarsResponse, Is.Not.Null);
            }
        }


        [Test, TestCaseSource("RestClients")]
        public void Can_GET_SearchReqstars_aged_20(IRestClient client)
        {
            var response = client.Get<ReqstarsResponse>("/reqstars/aged/20");
            Assert.That(response.Results.Count,
                Is.EqualTo(SeedData.Reqstars.Count(x => x.Age == 20)));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Disallows_SEND_SearchReqstars_aged_20_PrettyTypedApi(IServiceClient client)
        {
            try
            {
                var response = client.Send(new SearchReqstars { Age = 20 });
                Assert.Fail("POST's to SearchReqstars should not be allowed");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(405));
                Assert.That(webEx.StatusDescription, Is.EqualTo("Method Not Allowed"));
            }
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_GET_SearchReqstars_aged_20_PrettyRestApi(IRestClient client)
        {
            var request = new SearchReqstars { Age = 20 };
            var response = client.Get(request);

            Assert.That(request.ToUrl("GET"), Is.EqualTo("/reqstars/aged/20"));
            Assert.That(response.Results.Count,
                Is.EqualTo(SeedData.Reqstars.Count(x => x.Age == 20)));
        }


        [Test, TestCaseSource("RestClients")]
        public void Can_GET_GetReqstar(IRestClient client)
        {
            var response = client.Get<Reqstar>("/reqstars/1");
            Assert.That(response.FirstName,
                Is.EqualTo(SeedData.Reqstars[0].FirstName));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_GET_GetReqstar_PrettyRestApi(IRestClient client)
        {
            var request = new GetReqstar { Id = 1 };
            var response = client.Get(request);

            Assert.That(request.ToUrl("GET"), Is.EqualTo("/reqstars/1"));
            Assert.That(response.FirstName,
                Is.EqualTo(SeedData.Reqstars[0].FirstName));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Disallows_SEND_GetReqstar_PrettyTypedApi(IServiceClient client)
        {
            try
            {
                var response = client.Send(new GetReqstar { Id = 1 });
                Assert.Fail("POST's to GetReqstar should not be allowed");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(405));
                Assert.That(webEx.StatusDescription, Is.EqualTo("Method Not Allowed"));
            }
        }

        [Test]
        public void Can_GET_GetReqstar_View()
        {
            var html = "{0}/reqstars/1".Fmt(Host).GetStringFromUrl(accept: "text/html");
            html.Print();
            Assert.That(html, Does.Contain("<!--view:GetReqstar.cshtml-->"));
            Assert.That(html, Does.Contain("<!--template:HtmlReport.cshtml-->"));
        }


        public class EmptyResponse { }

        public List<Reqstar> GetAllReqstars(IRestClient client)
        {
            var response = client.Get(new AllReqstars());
            return response;
        }

        public List<Reqstar> GetAllReqstars(IServiceClient client)
        {
            return client.Send(new AllReqstars());
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_DELETE_Reqstar(IRestClient client)
        {
            var response = client.Delete<EmptyResponse>("/reqstars/1/delete");

            var reqstarsLeft = GetAllReqstars(client);

            Assert.That(reqstarsLeft.Count,
                Is.EqualTo(SeedData.Reqstars.Length - 1));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Can_DELETE_Reqstar_PrettyTypedApi(IServiceClient client)
        {
            client.Send(new DeleteReqstar { Id = 1 });

            var reqstarsLeft = GetAllReqstars(client);

            Assert.That(reqstarsLeft.Count,
                Is.EqualTo(SeedData.Reqstars.Length - 1));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_DELETE_Reqstar_PrettyRestApi(IRestClient client)
        {
            client.Delete(new DeleteReqstar { Id = 1 });

            var reqstarsLeft = GetAllReqstars(client);

            Assert.That(reqstarsLeft.Count,
                Is.EqualTo(SeedData.Reqstars.Length - 1));
        }


        [Test, TestCaseSource("RestClients")]
        public void Can_PATCH_UpdateReqstar(IRestClient client)
        {
            var response = client.Patch<Reqstar>("/reqstars/1", new UpdateReqstar(1, 18));
            Assert.That(response.Age, Is.EqualTo(18));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_PATCH_UpdateReqstar_PrettyRestApi(IRestClient client)
        {
            var response = client.Patch(new UpdateReqstar(1, 18));
            Assert.That(response.Age, Is.EqualTo(18));
        }


        [Test, TestCaseSource("RestClients")]
        public void Can_CREATE_Reqstar(IRestClient client)
        {
            var response = client.Post<List<Reqstar>>("/reqstars", new Reqstar(4, "Just", "Created", 25));

            Assert.That(response.Count,
                Is.EqualTo(SeedData.Reqstars.Length + 1));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Can_CREATE_Reqstar_PrettyTypedApi(IServiceClient client)
        {
            var response = client.Send(new Reqstar(4, "Just", "Created", 25));

            Assert.That(response.Count,
                Is.EqualTo(SeedData.Reqstars.Length + 1));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_CREATE_Reqstar_PrettyRestApi(IRestClient client)
        {
            var response = client.Post(new Reqstar(4, "Just", "Created", 25));

            Assert.That(response.Count,
                Is.EqualTo(SeedData.Reqstars.Length + 1));
        }

        [Test, TestCaseSource("RestClients")]
        public void Fails_to_CREATE_Empty_Reqstar_PrettyRestApi(IRestClient client)
        {
            try
            {
                var response = client.Post(new Reqstar());
                Assert.Fail("Should've thrown 400 Bad Request Error");
            }
            catch (WebServiceException webEx)
            {
                Assert.That(webEx.StatusCode, Is.EqualTo(400));
                Assert.That(webEx.StatusDescription, Is.EqualTo("ArgumentException"));

                Assert.That(webEx.ResponseStatus.ErrorCode, Is.EqualTo("ArgumentException"));
                Assert.That(webEx.ResponseStatus.Message, Is.EqualTo("Age is required"));

                Assert.That(webEx.ResponseDto as ErrorResponse, Is.Not.Null);
            }
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_GET_ResetReqstars(IRestClient client)
        {
            var response = client.Get<EmptyResponse>("/reqstars/reset");

            var reqstarsLeft = GetAllReqstars(client);

            Assert.That(reqstarsLeft.Count, Is.EqualTo(SeedData.Reqstars.Length));
        }

        [Test, TestCaseSource("ServiceClients")]
        public void Can_SEND_ResetReqstars_PrettyTypedApi(IServiceClient client)
        {
            client.Send(new ResetReqstar());

            var reqstarsLeft = GetAllReqstars(client);

            Assert.That(reqstarsLeft.Count, Is.EqualTo(SeedData.Reqstars.Length));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_GET_ResetReqstars_PrettyRestApi(IRestClient client)
        {
            client.Get(new ResetReqstar());

            var reqstarsLeft = GetAllReqstars(client);

            Assert.That(reqstarsLeft.Count, Is.EqualTo(SeedData.Reqstars.Length));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_GET_RoutelessReqstar_PrettyRestApi(IRestClient client)
        {
            var request = new RoutelessReqstar
            {
                Id = 1,
                FirstName = "Foo",
                LastName = "Bar",
            };

            var response = client.Get(request);

            var format = ((ServiceClientBase)client).Format;
            Assert.That(request.ToUrl("GET", format), Is.EqualTo(
                "/{0}/reply/RoutelessReqstar?id=1&firstName=Foo&lastName=Bar".Fmt(format)));
            Assert.That(response.Id, Is.EqualTo(request.Id));
            Assert.That(response.FirstName, Is.EqualTo(request.FirstName));
            Assert.That(response.LastName, Is.EqualTo(request.LastName));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_POST_RoutelessReqstar_PrettyRestApi(IRestClient client)
        {
            var request = new RoutelessReqstar
            {
                Id = 1,
                FirstName = "Foo",
                LastName = "Bar",
            };

            var response = client.Post(request);

            var format = ((ServiceClientBase)client).Format;
            Assert.That(request.ToUrl("POST", format), Is.EqualTo(
                "/{0}/reply/RoutelessReqstar".Fmt(format)));
            Assert.That(response.Id, Is.EqualTo(request.Id));
            Assert.That(response.FirstName, Is.EqualTo(request.FirstName));
            Assert.That(response.LastName, Is.EqualTo(request.LastName));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_download_Headers_response(IRestClient client)
        {
            using (HttpWebResponse response = client.Get(new Headers { Text = "Test" }))
                Assert.That(response.Headers["X-Response"], Is.EqualTo("Test"));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_download_Strings_response(IRestClient client)
        {
            string response = client.Get(new Strings { Text = "Test" });
            Assert.That(response, Is.EqualTo("Hello, Test"));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_download_Bytes_response(IRestClient client)
        {
            var guid = Guid.NewGuid();
            byte[] response = client.Get(new Bytes { Text = guid.ToString() });
            Assert.That(new Guid(response), Is.EqualTo(guid));
        }

        [Test, TestCaseSource("RestClients")]
        public void Can_download_Streams_response(IRestClient client)
        {
            var guid = Guid.NewGuid();
            using (Stream response = client.Get(new Streams { Text = guid.ToString() }))
            using (response)
            {
                var bytes = response.ReadFully();
                Assert.That(new Guid(bytes), Is.EqualTo(guid));
            }
        }
    }
}