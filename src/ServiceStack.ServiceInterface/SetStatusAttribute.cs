using System;
using System.Net;
using ServiceStack.Server;
using ServiceStack.ServiceHost;

namespace ServiceStack.ServiceInterface
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class SetStatusAttribute : RequestFilterAttribute
    {
        public int? Status { get; set; }
        public HttpStatusCode? StatusCode { get; set; }
        public string Description { get; set; }

        public SetStatusAttribute() {}

        public SetStatusAttribute(int status, string description)
        {
            Status = status;
            Description = description;
        }

        public SetStatusAttribute(HttpStatusCode statusCode, string description)
        : this((int)statusCode, description) {}

        public override void Execute(IHttpRequest req, IHttpResponse res, object requestDto)
        {
            if (Status.HasValue)
                res.StatusCode = Status.Value;

            if (!string.IsNullOrEmpty(Description))
                res.StatusDescription = Description;
        }
    }
}