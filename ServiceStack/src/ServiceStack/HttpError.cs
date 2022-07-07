using System;
using System.Collections.Generic;
using System.Net;
using ServiceStack.Model;
using ServiceStack.Validation;
using ServiceStack.Web;

namespace ServiceStack
{
    public class HttpError : Exception, IHttpError, IResponseStatusConvertible, IHasErrorCode, IHasResponseStatus
    {
        public HttpError() : this(null) { }

        public HttpError(string message)
            : this(HttpStatusCode.InternalServerError, message)
        { }

        public HttpError(HttpStatusCode statusCode)
            : this(statusCode, statusCode.ToString(), null)
        { }

        public HttpError(HttpStatusCode statusCode, string errorMessage)
            : this(statusCode, statusCode.ToString(), errorMessage)
        { }

        public HttpError(int statusCode, string errorCode)
            : this(statusCode, errorCode, null)
        { }
        
        public HttpError(ResponseStatus responseStatus, HttpStatusCode statusCode) 
            : this(new ErrorResponse { ResponseStatus = responseStatus }, statusCode, responseStatus.ErrorCode, responseStatus.Message) {}

        public HttpError(IHasResponseStatus responseDto, HttpStatusCode statusCode) 
            : this(responseDto, statusCode, responseDto.ResponseStatus.ErrorCode, responseDto.ResponseStatus.Message) {}

        public HttpError(object responseDto, HttpStatusCode statusCode, string errorCode, string errorMessage)
            : this(statusCode, errorCode, errorMessage)
        {
            this.Response = responseDto;
        }

        public HttpError(object responseDto, int statusCode, string errorCode, string errorMessage = null, Exception innerException = null)
            : this(statusCode, errorCode, errorMessage, innerException)
        {
            this.Response = responseDto;
        }

        public HttpError(HttpStatusCode statusCode, string errorCode, string errorMessage)
            : this((int)statusCode, errorCode, errorMessage)
        { }

        public HttpError(int statusCode, string errorCode, string errorMessage, Exception innerException = null)
            : base(errorMessage ?? errorCode ?? statusCode.ToString(), innerException)
        {
            this.ErrorCode = errorCode ?? statusCode.ToString();
            this.Status = statusCode;
            this.Headers = new Dictionary<string, string>();
            this.StatusDescription = innerException is IHasStatusDescription hasStatusDesc 
                ? hasStatusDesc.StatusDescription 
                : errorCode;
            this.Headers = new Dictionary<string, string>();
            this.Cookies = new List<Cookie>();
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
            this.Cookies = new List<Cookie>();
        }

        public string ErrorCode { get; set; }
        public string StackTrace { get; set; }

        public string ContentType { get; set; }

        public Dictionary<string, string> Headers { get; private set; }
        public List<Cookie> Cookies { get; private set; }

        public int Status { get; set; }

        public HttpStatusCode StatusCode
        {
            get => (HttpStatusCode)Status;
            set => Status = (int)value;
        }

        public string StatusDescription { get; set; }

        public object Response { get; set; }

        public IContentTypeWriter ResponseFilter { get; set; }

        public IRequest RequestContext { get; set; }

        public int PaddingLength { get; set; }

        public Func<IDisposable> ResultScope { get; set; }

        public IDictionary<string, string> Options => this.Headers;

        private ResponseStatus responseStatus;
        public ResponseStatus ResponseStatus
        {
            get => responseStatus ?? this.Response.GetResponseStatus();
            set => responseStatus = value;
        }

        public List<ResponseError> GetFieldErrors()
        {
            var responseStatus = ResponseStatus;
            if (responseStatus != null)
                return responseStatus.Errors ?? new List<ResponseError>();

            return new List<ResponseError>();
        }

        public static Exception NotFound(string message) => new HttpError(HttpStatusCode.NotFound, message);
        public static Exception Unauthorized(string message) => new HttpError(HttpStatusCode.Unauthorized, message);
        public static Exception Conflict(string message) => new HttpError(HttpStatusCode.Conflict, message);
        public static Exception Forbidden(string message) => new HttpError(HttpStatusCode.Forbidden, message);
        public static Exception MethodNotAllowed(string message) => new HttpError(HttpStatusCode.MethodNotAllowed, message);
        public static Exception BadRequest(string message) => new HttpError(HttpStatusCode.BadRequest, message);
        public static Exception BadRequest(string errorCode, string message) => new HttpError(HttpStatusCode.BadRequest, errorCode, message);
        public static Exception PreconditionFailed(string message) => new HttpError(HttpStatusCode.PreconditionFailed, message);
        public static Exception ExpectationFailed(string message) => new HttpError(HttpStatusCode.ExpectationFailed, message);
        public static Exception NotImplemented(string message) => new HttpError(HttpStatusCode.NotImplemented, message);
        public static Exception ServiceUnavailable(string message) => new HttpError(HttpStatusCode.ServiceUnavailable, message);

        public static Exception Validation(string errorCode, string errorMessage, string fieldName) => 
            ValidationError.CreateException(errorCode, errorMessage, fieldName);

        public ResponseStatus ToResponseStatus() => Response.GetResponseStatus()
            ?? ResponseStatusUtils.CreateResponseStatus(ErrorCode, Message, null);

        public static Exception GetException(object responseDto)
        {
            if (responseDto is Exception e)
                return e;
            
            if (responseDto is IHttpError httpError)
                return new HttpError(HttpStatusCode.InternalServerError, httpError.Message) {
                    ErrorCode = httpError.ErrorCode,
                    StackTrace = httpError.StackTrace,
                };

            var status = responseDto.GetResponseStatus();
            if (status?.ErrorCode != null)
                return new HttpError(HttpStatusCode.InternalServerError, status.Message ?? status.ErrorCode) {
                    ErrorCode = status.ErrorCode,
                    StackTrace = status.StackTrace,
                };
            
            return null;
        }
    }
}