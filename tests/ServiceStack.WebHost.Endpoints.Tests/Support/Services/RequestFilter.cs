using System;
using System.Runtime.Serialization;
using ServiceStack.Server;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	[DataContract]
	public class RequestFilter
	{
		[DataMember]
		public int StatusCode { get; set; }

		[DataMember]
		public string HeaderName { get; set; }

		[DataMember]
		public string HeaderValue { get; set; }
	}

	[DataContract]
	public class RequestFilterResponse
	{
		[DataMember]
		public string Value { get; set; }
	}

	public class StatusCodeService
		: TestServiceBase<RequestFilter>, IRequiresRequestContext
	{
		public IRequestContext RequestContext { get; set; }

		protected override object Run(RequestFilter request)
		{
			return new RequestFilterResponse();
		}
	}

}