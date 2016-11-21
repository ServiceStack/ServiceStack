using System;
using System.Runtime.Serialization;
using ServiceStack.Web;

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
        : TestServiceBase<Headers>, IRequiresRequest
    {
        public IRequest Request { get; set; }

        protected override object Run(Headers request)
        {
            var header = Request.GetHeader(request.Name);

            return new HeadersResponse
            {
                Value = header
            };
        }
    }
}