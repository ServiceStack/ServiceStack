using System;
using System.Runtime.Serialization;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.ServiceModel;

namespace ServiceStack.WebHost.IntegrationTests.Services
{
	[DataContract]
	public class AlwaysThrows
	{
	    [DataMember]
	    public int? StatusCode { get; set; }
		[DataMember]
		public string Value { get; set; }
	}

	[DataContract]
	public class AlwaysThrowsResponse
		: IHasResponseStatus
	{
		public AlwaysThrowsResponse()
		{
			this.ResponseStatus = new ResponseStatus();
		}

		[DataMember]
		public string Result { get; set; }

		[DataMember]
		public ResponseStatus ResponseStatus { get; set; }
	}

	public class AlwaysThrowsService 
		: ServiceInterface.Service
	{
		public object Any(AlwaysThrows request)
		{
            if (request.StatusCode.HasValue)
            {
                throw new HttpError(
                    request.StatusCode.Value,
                    typeof(NotImplementedException).Name,
                    request.Value);
            }

			throw new NotImplementedException(GetErrorMessage(request.Value));
		}

		public static string GetErrorMessage(string value)
		{
			return value + " is not implemented";
		}
	}
}