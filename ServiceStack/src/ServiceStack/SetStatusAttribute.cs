using System;
using System.Net;
using ServiceStack.Web;

namespace ServiceStack
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class SetStatusAttribute : RequestFilterAttribute
    {
        public int? Status { get; set; }
        public HttpStatusCode? StatusCode { get; set; }
        public string Description { get; set; }

        public SetStatusAttribute() { }

        public SetStatusAttribute(int status, string description)
        {
            Status = status;
            Description = description;
        }

        public SetStatusAttribute(HttpStatusCode statusCode, string description)
        : this((int)statusCode, description)
        { }

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            if (Status.HasValue)
                res.StatusCode = Status.Value;

            if (!string.IsNullOrEmpty(Description))
                res.StatusDescription = Description;
        }
    }
}