using System;
using System.Runtime.Serialization;
using ServiceStack.ServiceHost;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
	[DataContract]
	public class Headers
	{
		[DataMember]
		public string Name { get; set; }
	}

	[DataContract]
	public class HeadersResponse
	{
		[DataMember]
		public string Value { get; set; }
	}

	public class HeadersService
		: TestServiceBase<Headers>, IRequiresRequestContext
	{
		public IRequestContext RequestContext { get; set; }

		protected override object Run(Headers request)
		{
			return new HeadersResponse
			{
				Value = RequestContext.GetHeader(request.Name)
			};
		}
	}

}