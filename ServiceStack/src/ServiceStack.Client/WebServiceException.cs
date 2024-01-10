// Copyright (c) ServiceStack, Inc. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Net;
using ServiceStack.Logging;
using ServiceStack.Model;
using ServiceStack.Text;

namespace ServiceStack
{
    [Serializable]
    public class WebServiceException
        : Exception, IHasStatusCode, IHasStatusDescription, IResponseStatusConvertible, IHasResponseStatus
    {
        public WebServiceException() { }
        public WebServiceException(string message) : base(message) { }
        public WebServiceException(string message, Exception innerException) : base(message, innerException) { }

        public int StatusCode { get; set; } = 500;

        public string StatusDescription { get; set; }

        public WebHeaderCollection ResponseHeaders { get; set; }

        public object ResponseDto { get; set; }

        public string ResponseBody { get; set; }

        public override string Message => ErrorMessage ?? base.Message;

        public string ErrorCode => ResponseStatus?.ErrorCode ?? StatusDescription;

        public string ErrorMessage => ResponseStatus?.Message;

        public string ServerStackTrace => ResponseStatus?.StackTrace;
        
        public object State { get; set; }

        private ResponseStatus responseStatus = null;
        public ResponseStatus ResponseStatus
        {
            get
            {
                if (responseStatus != null)
                    return responseStatus;
                
                if (this.ResponseDto is IHasResponseStatus hasResponseStatus)
                    responseStatus = hasResponseStatus.ResponseStatus;

                try
                {
                    if (responseStatus == null && ResponseDto != null)
                    {
                        var propertyInfo = this.ResponseDto.GetType().GetProperty(nameof(ResponseStatus));
                        
                        var statusDto = propertyInfo?.GetProperty(this.ResponseDto);
                        responseStatus = ToBuiltInResponseStatus(statusDto);

                        if (responseStatus == null)
                        {
                            if (ResponseDto.ToObjectDictionary().TryGetValue(nameof(ResponseStatus), out var oStatus))
                                responseStatus = ToBuiltInResponseStatus(oStatus);
                        }
                    }

                    if (responseStatus == null && ResponseBody != null)
                    {
                        var tryJsonFirst = ResponseBody.StartsWith("{\"");

                        var errorResponse = tryJsonFirst ? ResponseBody.FromJson<ErrorResponse>() : null;
                        if (errorResponse?.ResponseStatus?.ErrorCode == null)
                            errorResponse = ResponseBody.FromJsv<ErrorResponse>();
                        if (!tryJsonFirst && errorResponse?.ResponseStatus?.ErrorCode == null)
                            errorResponse = ResponseBody.FromJson<ErrorResponse>();

                        if (errorResponse?.ResponseStatus?.ErrorCode != null)
                            responseStatus = errorResponse?.ResponseStatus;
                    }
                }
                catch (Exception ex)
                {
                    var log = LogManager.GetLogger(typeof(WebServiceException));
                    if (log.IsDebugEnabled)
                        log.Debug($"Could not parse Error ResponseStatus {ResponseDto?.GetType().Name}", ex);
                }

                if (string.IsNullOrEmpty(responseStatus?.ErrorCode))
                {
                    if (InnerException != null)
                    {
                        responseStatus = InnerException is not IResponseStatusConvertible // avoid potential infinite recursion
                            ? ErrorUtils.CreateError(InnerException)
                            : ErrorUtils.CreateError(InnerException.Message, InnerException.GetType().Name);
                    }
                    else
                    {
                        var message = StatusDescription ?? base.Message ?? ((HttpStatusCode)StatusCode).ToString().SplitCamelCase();
                        responseStatus = ErrorUtils.CreateError(message, errorCode:message);
                    }
                }

                return responseStatus;
            }
            set => responseStatus = value;
        }

        private ResponseStatus ToBuiltInResponseStatus(object statusDto)
        {
            responseStatus = statusDto as ResponseStatus;
            if (responseStatus != null)
                return responseStatus;

            // Generated DTO
            return statusDto?.GetType().Name == nameof(IHasResponseStatus.ResponseStatus)
                ? statusDto.ConvertTo(typeof(ResponseStatus)) as ResponseStatus
                : responseStatus;
        }

        public ResponseStatus ToResponseStatus() => ResponseStatus;

        public List<ResponseError> GetFieldErrors() => ResponseStatus?.Errors ?? [];

        public bool IsAny400() => StatusCode is >= 400 and < 500;

        public bool IsAny500() => StatusCode is >= 500 and < 600;

        public override string ToString()
        {
            var sb = StringBuilderCache.Allocate();
            sb.Append($"{StatusCode} {StatusDescription}\n");
            sb.Append($"Code: {ErrorCode}, Message: {ErrorMessage}\n");

            var status = ResponseStatus;
            if (status != null)
            {
                if (!status.Errors.IsNullOrEmpty())
                {
                    sb.Append("Field Errors:\n");
                    foreach (var error in status.Errors)
                    {
                        sb.Append($"  [{error.FieldName}] {error.ErrorCode}: {error.Message}\n");

                        if (error.Meta != null && error.Meta.Count > 0)
                        {
                            sb.Append("  Field Meta:\n");
                            foreach (var entry in error.Meta)
                            {
                                sb.Append($"    {entry.Key}: {entry.Value}\n");
                            }
                        }
                    }
                }

                if (status.Meta != null && status.Meta.Count > 0)
                {
                    sb.Append("Meta:\n");
                    foreach (var entry in status.Meta)
                    {
                        sb.Append($"  {entry.Key}: {entry.Value}\n");
                    }
                }
            }

            if (!string.IsNullOrEmpty(ServerStackTrace))
                sb.Append($"Server StackTrace:\n {ServerStackTrace}\n");


            return StringBuilderCache.ReturnAndFree(sb);
        }
    }

    public class RefreshTokenException : WebServiceException
    {
        public RefreshTokenException(string message) : base(message) {}

        public RefreshTokenException(string message, Exception innerException) 
            : base(message, innerException) {}

        public RefreshTokenException(WebServiceException webEx) 
            : base(webEx.Message)
        {
            if (webEx == null)
                throw new ArgumentNullException(nameof(webEx));

            this.StatusCode = webEx.StatusCode;
            this.StatusDescription = webEx.StatusDescription;
            this.ResponseHeaders = webEx.ResponseHeaders;
            this.ResponseBody = webEx.ResponseBody;
            this.ResponseDto = webEx.ResponseDto;
        }
    }
}
