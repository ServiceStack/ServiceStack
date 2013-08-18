using System;
using System.Net;
using System.Runtime.Serialization;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
    [Route("/httpresults")]
	[DataContract]
	public class HttpResults
	{
		[DataMember]
		public string Name { get; set; }
	}

	[DataContract]
	public class HttpResultsResponse
		: IHasResponseStatus
	{
        public HttpResultsResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public string Result { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}

    public class HttpResultsService : ServiceInterface.Service
	{
        public object Any(HttpResults request)
		{
            if (request.Name == "Error")
                throw new HttpError(HttpStatusCode.NotFound, "Error NotFound");

            return new HttpResult(HttpStatusCode.NotFound, "Returned NotFound");
		}
	}
}