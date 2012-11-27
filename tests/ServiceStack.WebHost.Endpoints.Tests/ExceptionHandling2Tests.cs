using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProtoBuf;
using ServiceStack.Plugins.ProtoBuf;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.WebHost.Endpoints.Tests
{

    #region ServiceModel

    [Route("/reqstars")]
    [ProtoContract]
    public class Reqstar //: IReturn<List<Reqstar>>
    {
        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public string FirstName { get; set; }

        [ProtoMember(3)]
        public string LastName { get; set; }

        [ProtoMember(4)]
        public int? Age { get; set; }
    }

    //New: No special naming convention

    [Route("/reqstars2/search")]
    [Route("/reqstars2/aged/{Age}")]
    [ProtoContract]
    public class SearchReqstars2 : IReturn<ReqstarsResponse>
    {
        [ProtoMember(1)]
        public int? Age { get; set; }
    }

    [ProtoContract]
    public class ReqstarsResponse
    {
        [ProtoMember(1)]
        public int Total { get; set; }

        [ProtoMember(2)]
        public int? Aged { get; set; }

        [ProtoMember(3)]
        public List<Reqstar> Results { get; set; }
    }

    //Naming convention:{Request DTO}Response

    [Route("/reqstars/search")]
    [Route("/reqstars/aged/{Age}")]
    [ProtoContract]
    public class SearchReqstars : IReturn<SearchReqstarsResponse>
    {
        [ProtoMember(1)]
        public int? Age { get; set; }
    }


    [ProtoContract]
    public class SearchReqstarsResponse
    {
        [ProtoMember(1)]
        public int Total { get; set; }

        [ProtoMember(2)]
        public int? Aged { get; set; }

        [ProtoMember(3)]
        public List<Reqstar> Results { get; set; }

        [ProtoMember(4)]
        public ResponseStatus ResponseStatus { get; set; }
    }


    #endregion

    #region Service

    public class ReqstarsService : IService
    {
        /// <summary>
        /// Testmethod following the 'old' naming convention (DTO/DTOResponse)
        /// </summary>
        public object Any(SearchReqstars request)
        {
            if (request.Age.HasValue && request.Age <= 0)
                throw new ArgumentException("Invalid Age");

            var response = new SearchReqstarsResponse()
            {
                Total = 2,
                Aged = 10,
                Results = new List<Reqstar>()
                                {
                                    new Reqstar() { Id = 1, FirstName = "Max", LastName = "Meier", Age = 10 },
                                    new Reqstar() { Id = 2, FirstName = "Susan", LastName = "Stark", Age = 10 }
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
                Results = new List<Reqstar>()
                                {
                                    new Reqstar() { Id = 1, FirstName = "Max", LastName = "Meier", Age = 10 },
                                    new Reqstar() { Id = 2, FirstName = "Susan", LastName = "Stark", Age = 10 }
                                }
            };

            return response;
        }
    }

    #endregion

    #region AppHost

    public class AppHost : AppHostHttpListenerBase
    {
        public AppHost()
            : base("Test ErrorHandling", typeof(ReqstarsService).Assembly)
        {
        }

        public override void Configure(Funq.Container container)
        {
            Plugins.Add(new ProtoBufFormat());
        }
    }

    #endregion

    [TestFixture]
    public class ExceptionHandling2Tests
    {
        private static string _testUri = "http://localhost:1337/";

        #region Setup

        [TestFixtureSetUp]
        public static void Init()
        {
            try
            {
                var appHost = new AppHost();
                appHost.Init();
                EndpointHost.Config.DebugMode = true;
                appHost.Start("http://*:1337/");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        #endregion

        static IRestClient[] ServiceClients = 
		{
			new JsonServiceClient(_testUri),
			new XmlServiceClient(_testUri),
			new JsvServiceClient(_testUri),
			new ProtoBufServiceClient(_testUri)
		};


        #region Tests with old naming convention

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

        #endregion

        #region Tests with no special naming convention

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

        #endregion

    }

}
