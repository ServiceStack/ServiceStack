using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using ServiceStack.Model;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests.Support.Services
{
    [DataContract]
    [Route("/errors")]
    [Route("/errors/{Type}")]
    [Route("/errors/{Type}/{StatusCode}")]
    [Route("/errors/{Type}/{StatusCode}/{Message}")]
    public class ThrowHttpError : IReturn<ThrowHttpErrorResponse>
    {
        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public int? StatusCode { get; set; }
    }

    [DataContract]
    [Route("/errors")]
    [Route("/errors/{Type}")]
    [Route("/errors/{Type}/{StatusCode}")]
    [Route("/errors/{Type}/{StatusCode}/{Message}")]
    public class ThrowHttpErrorNoReturn : IReturnVoid
    {
        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public int? StatusCode { get; set; }
    }

    [DataContract]
    public class ThrowHttpErrorResponse
        : IHasResponseStatus
    {
        public ThrowHttpErrorResponse()
        {
            this.ResponseStatus = new ResponseStatus();
        }

        [DataMember]
        public ResponseStatus ResponseStatus { get; set; }
    }

    [Route("/throw404")]
    public class Throw404 { }

    [Route("/throw404description")]
    public class Throw404Description { }

    [Route("/throwcustom404")]
    public class ThrowCustom404 { }

    [Route("/return404")]
    public class Return404 { }

    [Route("/return404result")]
    public class Return404Result { }

    [Route("/throwwebex")]
    public class ThrowWebServiceException : IHasResponseStatus
    {
        public int? StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public string Message { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }

    public class Custom404Exception : Exception, IResponseStatusConvertible, IHasStatusCode
    {
        public Custom404Exception(string message) : base(message) {}

        public ResponseStatus ToResponseStatus()
        {
            return new ResponseStatus
            {
                ErrorCode = GetType().Name,
                Message = this.Message,
                Errors = new List<ResponseError>
                {
                    new ResponseError
                    {
                        ErrorCode = "FieldErrorCode",
                        Message = "FieldMessage",
                        FieldName = "FieldName",
                    }
                }
            };
        }

        public int StatusCode
        {
            get { return (int)HttpStatusCode.NotFound; }
        }
    }

    public class HttpErrorService : Service
    {
        public object Any(ThrowHttpError request)
        {
            if (request.Type.IsNullOrEmpty())
                throw new ArgumentNullException("Type");

            var ex = new Exception(request.Message);
            switch (request.Type)
            {
                case "FileNotFoundException":
                    ex = new FileNotFoundException(request.Message);
                    break;
            }

            if (!request.StatusCode.HasValue)
                throw ex;

            var httpStatus = (HttpStatusCode)request.StatusCode.Value;
            throw new HttpError(httpStatus, ex);
        }

        public object Any(Throw404 request)
        {
            throw HttpError.NotFound("Custom Status Description");
        }

        public object Any(Throw404Description request)
        {
            throw new HttpError(HttpStatusCode.NotFound) {
                StatusDescription = "Custom Status Description"
            };
        }

        public object Any(ThrowCustom404 request)
        {
            throw new Custom404Exception("Custom Status Description");
        }

        public object Any(Return404 request)
        {
            return HttpError.NotFound("Custom Status Description");
        }

        public object Any(Return404Result request)
        {
            return new HttpResult(HttpStatusCode.NotFound, "Custom Status Description");
        }

        public object Any(ThrowWebServiceException request)
        {
            throw new WebServiceException(request.Message ?? "Message")
            {
                StatusCode = request.StatusCode ?? 500,
                StatusDescription = request.StatusDescription ?? "StatusDescription",                
                ResponseDto = request,
            };
        }
    }

}