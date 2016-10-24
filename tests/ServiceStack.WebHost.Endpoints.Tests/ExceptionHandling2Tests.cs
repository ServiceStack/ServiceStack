using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using NUnit.Framework;
using ProtoBuf;
using ServiceStack.Text;
using ServiceStack.ProtoBuf;
#if !NETCORE_SUPPORT
using ServiceStack.ServiceModel;
#endif

namespace ServiceStack.WebHost.Endpoints.Tests
{

    [Route("/reqstars")]
    [DataContract]
    public class Reqstar //: IReturn<List<Reqstar>>
    {
        [DataMember(Order = 1)]
        public int Id { get; set; }

        [DataMember(Order = 2)]
        public string FirstName { get; set; }

        [DataMember(Order = 3)]
        public string LastName { get; set; }

        [DataMember(Order = 4)]
        public int? Age { get; set; }
    }

    //New: No special naming convention

    [Route("/reqstars2/search")]
    [Route("/reqstars2/aged/{Age}")]
    [DataContract]
    public class SearchReqstars2 : IReturn<ReqstarsResponse>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
    }

    [DataContract]
    public class ReqstarsResponse
    {
        [DataMember(Order = 1)]
        public int Total { get; set; }

        [DataMember(Order = 2)]
        public int? Aged { get; set; }

        [DataMember(Order = 3)]
        public List<Reqstar> Results { get; set; }

        [DataMember(Order = 4)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    //Naming convention:{Request DTO}Response

    [Route("/reqstars/search")]
    [Route("/reqstars/aged/{Age}")]
    [DataContract]
    public class SearchReqstars : IReturn<SearchReqstarsResponse>
    {
        [DataMember(Order = 1)]
        public int? Age { get; set; }
    }


    [DataContract]
    public class SearchReqstarsResponse
    {
        [DataMember(Order = 1)]
        public int Total { get; set; }

        [DataMember(Order = 2)]
        public int? Aged { get; set; }

        [DataMember(Order = 3)]
        public List<Reqstar> Results { get; set; }

        [DataMember(Order = 4)]
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class ReqstarsService : IService
    {
        /// <summary>
        /// Testmethod following the 'old' naming convention (DTO/DTOResponse)
        /// </summary>
        public object Any(SearchReqstars request)
        {
            if (request.Age.HasValue && request.Age <= 0)
                throw new ArgumentException("Invalid Age");

            var response = new SearchReqstarsResponse
            {
                Total = 2,
                Aged = 10,
                Results = new List<Reqstar> {
                    new Reqstar { Id = 1, FirstName = "Max", LastName = "Meier", Age = 10 },
                    new Reqstar { Id = 2, FirstName = "Susan", LastName = "Stark", Age = 10 }
                }
            };

            return response;
        }

        /// <summary>
        /// Testmethod following no special naming convention (the new behavior)
        /// </summary>
        public object Any(SearchReqstars2 request)
        {
            if (request.Age.HasValue && request.Age <= 0)
                throw new ArgumentException("Invalid Age");

            var response = new ReqstarsResponse()
            {
                Total = 2,
                Aged = 10,
                Results = new List<Reqstar> {
                    new Reqstar { Id = 1, FirstName = "Max", LastName = "Meier", Age = 10 },
                    new Reqstar { Id = 2, FirstName = "Susan", LastName = "Stark", Age = 10 }
                }
            };

            return response;
        }
    }

    public class AppHost : AppHostHttpListenerBase
    {
        public AppHost()
            : base("Test ErrorHandling", typeof(ReqstarsService).GetAssembly())
        {
        }

        public override void Configure(Funq.Container container)
        {
            Plugins.Add(new ProtoBufFormat());
        }
    }

    [TestFixture]
    public class ExceptionHandling2Tests
    {
        private static string testUri = Config.ListeningOn;

        AppHost appHost;

        [TestFixtureSetUp]
        public void Init()
        {
            try
            {
                appHost = new AppHost();
                appHost.Init();
                appHost.Start(Config.ListeningOn);
                appHost.Config.DebugMode = true;
            }
            catch (Exception ex)
            {
                ex.ToString().Print();
            }
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            appHost.Dispose();
        }

        static IRestClient[] ServiceClients =
        {
            new JsonServiceClient(testUri),
            new JsonHttpClient(testUri),
            new XmlServiceClient(testUri),
            new JsvServiceClient(testUri),
            new ProtoBufServiceClient(testUri)
        };


        /// <summary>
        ///A test for a good response
        ///</summary>
        [Test, TestCaseSource("ServiceClients")]
        [Category("OldNamingConvention")]
        public void OldNamingConv_Get_ExpectingResults(IRestClient client)
        {
            var response = client.Get(new SearchReqstars { Age = 10 });

            Assert.AreEqual(2, response.Total);
        }

        /// <summary>
        ///A GET test for receiving a WebServiceException with status ArgumentException and message "Invalid Age"
        ///</summary>
        [Test, TestCaseSource("ServiceClients")]
        [Category("OldNamingConvention")]
        public void OldNamingConv_Get_ArgumentException_InvalidAge(IRestClient client)
        {
            try
            {
                client.Get(new SearchReqstars { Age = -1 });
            }
            catch (WebServiceException ex)
            {
                Assert.AreEqual("ArgumentException", ex.StatusDescription, "Wrong ExceptionType");
                Assert.AreEqual("Invalid Age", ex.ErrorMessage, "Wrong message");
            }
        }

        /// <summary>
        ///A test for a good response with POST request
        ///</summary>
        [Test, TestCaseSource("ServiceClients")]
        [Category("OldNamingConvention")]
        public void OldNamingConv_Post_ExpectingResults(IRestClient client)
        {
            var response = client.Post(new SearchReqstars { Age = 10 });

            Assert.AreEqual(2, response.Total);
        }

        /// <summary>
        ///A POST test for receiving a WebServiceException with status ArgumentException and message "Invalid Age"
        ///</summary>
        [Test, TestCaseSource("ServiceClients")]
        [Category("OldNamingConvention")]
        public void OldNamingConv_Post_ArgumentException_InvalidAge(IRestClient client)
        {
            try
            {
                client.Post(new SearchReqstars { Age = -1 });
            }
            catch (WebServiceException ex)
            {
                Assert.AreEqual("ArgumentException", ex.StatusDescription, "Wrong ExceptionType");
                Assert.AreEqual("Invalid Age", ex.ErrorMessage, "Wrong message");
            }
        }


        /// <summary>
        ///A test for a good response
        ///</summary>
        [Test, TestCaseSource("ServiceClients")]
        [Category("NoNamingConvention")]
        public void NoNamingConv_Get_ExpectingResults(IRestClient client)
        {
            var response = client.Get(new SearchReqstars2 { Age = 10 });

            Assert.AreEqual(2, response.Total);
        }

        /// <summary>
        ///A GET test for receiving a WebServiceException with status ArgumentException and message "Invalid Age"
        ///</summary>
        [Test, TestCaseSource("ServiceClients")]
        [Category("NoNamingConvention")]
        public void NoNamingConv_Get_ArgumentException_InvalidAge(IRestClient client)
        {
            try
            {
                client.Get(new SearchReqstars2 { Age = -1 });
            }
            catch (WebServiceException ex)
            {
                Assert.AreEqual("ArgumentException", ex.StatusDescription, "Wrong ExceptionType");
                Assert.AreEqual("Invalid Age", ex.ErrorMessage, "Wrong message");
            }
        }

        /// <summary>
        ///A test for a good response with POST request
        ///</summary>
        [Test, TestCaseSource("ServiceClients")]
        [Category("NoNamingConvention")]
        public void NoNamingConv_Post_ExpectingResults(IRestClient client)
        {
            var response = client.Post(new SearchReqstars2 { Age = 10 });

            Assert.AreEqual(2, response.Total);
        }

        /// <summary>
        ///A POST test for receiving a WebServiceException with status ArgumentException and message "Invalid Age"
        ///</summary>
        [Test, TestCaseSource("ServiceClients")]
        [Category("NoNamingConvention")]
        public void NoNamingConv_Post_ArgumentException_InvalidAge(IRestClient client)
        {
            try
            {
                client.Post(new SearchReqstars2 { Age = -1 });
            }
            catch (WebServiceException ex)
            {
                Assert.AreEqual("ArgumentException", ex.StatusDescription, "Wrong ExceptionType");
                Assert.AreEqual("Invalid Age", ex.ErrorMessage, "Wrong message");
            }
        }

    }
}
