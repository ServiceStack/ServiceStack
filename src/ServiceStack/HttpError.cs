using System;
using System.Collections.Generic;
using System.Net;
using ServiceStack.Model;
using ServiceStack.Web;

namespace ServiceStack
{
    public class HttpError : Exception, IHttpError, IResponseStatusConvertible
    {
        public HttpError() : this(null) {}

        public HttpError(string message)
            : this(HttpStatusCode.InternalServerError, message) {}

        public HttpError(HttpStatusCode statusCode, string errorCode)
            : this(statusCode, errorCode, null) { }

        public HttpError(int statusCode, string errorCode)
            : this(statusCode, errorCode, null) { }

        public HttpError(object responseDto, HttpStatusCode statusCode, string errorCode, string errorMessage)
            : this(statusCode, errorCode, errorMessage)
        {
            this.Response = responseDto;
        }

        public HttpError(object responseDto, int statusCode, string errorCode, string errorMessage = null, Exception innerException=null)
            : this(statusCode, errorCode, errorMessage, innerException)
        {
            this.Response = responseDto;
        }

        public HttpError(HttpStatusCode statusCode, string errorCode, string errorMessage)
            : this((int)statusCode, errorCode, errorMessage){}

        public HttpError(int statusCode, string errorCode, string errorMessage, Exception innerException = null)
            : base(errorMessage ?? errorCode ?? statusCode.ToString(), innerException)
        {
            this.ErrorCode = errorCode ?? statusCode.ToString();
            this.Status = statusCode;
            this.Headers = new Dictionary<string, string>();
            this.StatusDescription = errorCode;
        }

        public HttpError(HttpStatusCode statusCode, Exception innerException)
            : this(innerException.Message, innerException)
        {
            this.StatusCode = statusCode;
        }

        public HttpError(string message, Exception innerException) : base(message, innerException)
        {
            if (innerException != null)
            {
                this.ErrorCode = innerException.GetType().Name;
            }
            this.Headers = new Dictionary<string, string>();			
        }

        public string ErrorCode { get; set; }

        public string ContentType { get; set; }

        public Dictionary<string, string> Headers { get; set; }
        
        public int Status { get; set; }

        public HttpStatusCode StatusCode
        {
            get { return (HttpStatusCode)Status; }
            set { Status = (int)value; }
        }

        public string StatusDescription { get; set; }

        public object Response { get; set; }

        public IContentTypeWriter ResponseFilter { get; set; }
        
        public IRequest RequestContext { get; set; }

        public int PaddingLength { get; set; }

        public IDictionary<string, string> Options
        {
            get { return this.Headers; }
        }

        public ResponseStatus ResponseStatus
        {
            get
            {
                return this.Response.GetResponseStatus();
            }
        }

        public List<ResponseError> GetFieldErrors()
        {
            var responseStatus = ResponseStatus;
            if (responseStatus != null)
                return responseStatus.Errors ?? new List<ResponseError>();
            
            return new List<ResponseError>();
        }

        public static Exception NotFound(string message)
        {
            return new HttpError(HttpStatusCode.NotFound, message);
        }

        public static Exception Unauthorized(string message)
        {
            return new HttpError(HttpStatusCode.Unauthorized, message);
        }

        public static Exception Conflict(string message)
        {
            return new HttpError(HttpStatusCode.Conflict, message);
        }

        public static Exception Forbidden(string message)
        {
            return new HttpError(HttpStatusCode.Forbidden, message);
        }

        public ResponseStatus ToResponseStatus()
        {
            return Response.GetResponseStatus()
                ?? ResponseStatusUtils.CreateResponseStatus(ErrorCode, Message, null);
        }
    }
}