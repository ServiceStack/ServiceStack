using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace ServiceStack
{
    [Route("/requests/canceled")]
    [DataContract]
    public class CancelRequest : IReturn<CancelRequestResponse>
    {
        public const string CancelRequestIdHeader = "X-SS-RequestId";

        [DataMember(Order = 1)]
        public Guid RequestId { get; set; }
    }

    [DataContract]
    public class CancelRequestResponse
    {
        [DataMember(Order = 1)]
        public ResponseStatus ResponseStatus { get; set; }

        public CancelRequestResponse()
        {
            ResponseStatus = new ResponseStatus();
        }
    }
}
