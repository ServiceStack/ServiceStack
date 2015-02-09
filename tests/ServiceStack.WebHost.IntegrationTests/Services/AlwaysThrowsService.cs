using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.FluentValidation;
using ServiceStack.Web;

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

    [Route("/throwslist/{StatusCode}/{Value}")]
    [DataContract]
    public class AlwaysThrowsList
    {
        [DataMember]
        public int? StatusCode { get; set; }
        [DataMember]
        public string Value { get; set; }
    }

    [Route("/throwsvalidation")]
    [DataContract]
    public class AlwaysThrowsValidation
    {
        [DataMember]
        public string Value { get; set; }
    }

    public class AlwaysThrowsValidator : AbstractValidator<AlwaysThrowsValidation>
    {
        public AlwaysThrowsValidator()
        {
            RuleFor(x => x.Value).NotEmpty();
        }
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

	public class AlwaysThrowsService : Service
	{
        public object Any(AlwaysThrows request)
        {
            if (request.StatusCode.HasValue)
            {
                throw new HttpError(
                    request.StatusCode.Value,
                    typeof(NotImplementedException).Name,
                    GetErrorMessage(request.Value));
            }

            throw new NotImplementedException(GetErrorMessage(request.Value));
        }

        public List<AlwaysThrows> Any(AlwaysThrowsList request)
        {
            Any(request.ConvertTo<AlwaysThrows>());

            return new List<AlwaysThrows>();
        }

        public List<AlwaysThrows> Any(AlwaysThrowsValidation request)
        {
            return new List<AlwaysThrows>();
        }

        public static string GetErrorMessage(string value)
		{
			return value + " is not implemented";
		}
	}
}