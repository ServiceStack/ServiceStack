using System;
using System.Collections.Generic;
using ServiceStack.Common.Utils;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
{
    public class WebServiceException
        : Exception
    {
        public WebServiceException() { }
        public WebServiceException(string message) : base(message) { }
        public WebServiceException(string message, Exception innerException) : base(message, innerException) { }

        public int StatusCode { get; set; }

        public string StatusDescription { get; set; }

        public object ResponseDto { get; set; }
        
        public string ResponseBody { get; set; }

        private string errorCode;

        private void ParseResponseDto()
        {
            if (ResponseDto == null)
            {
                errorCode = StatusDescription;
                return;
            }
            var jsv = TypeSerializer.SerializeToString(ResponseDto);
            var map = TypeSerializer.DeserializeFromString<Dictionary<string, string>>(jsv);
            map = new Dictionary<string, string>(map, StringComparer.InvariantCultureIgnoreCase);
            string responseStatus;
            if (!map.TryGetValue("ResponseStatus", out responseStatus)) return;

            var rsMap = TypeSerializer.DeserializeFromString<Dictionary<string, string>>(responseStatus);
            if (rsMap == null) return;
            rsMap = new Dictionary<string, string>(rsMap, StringComparer.InvariantCultureIgnoreCase);
            rsMap.TryGetValue("ErrorCode", out errorCode);
            rsMap.TryGetValue("Message", out errorMessage);
            rsMap.TryGetValue("StackTrace", out serverStackTrace);
        }

        public string ErrorCode
        {
            get
            {
                if (errorCode == null)
                {
                    ParseResponseDto();
                }
                return errorCode;
            }
        }

        private string errorMessage;
        public string ErrorMessage
        {
            get
            {
                if (errorMessage == null)
                {
                    ParseResponseDto();
                }
                return errorMessage;
            }
        }

        private string serverStackTrace;
        public string ServerStackTrace
        {
            get
            {
                if (serverStackTrace == null)
                {
                    ParseResponseDto();
                }
                return serverStackTrace;
            }
        }

        public ResponseStatus ResponseStatus
        {
            get
            {
                if (this.ResponseDto == null)
                    return null;

                var hasResponseStatus = this.ResponseDto as IHasResponseStatus;
                if (hasResponseStatus != null)
                    return hasResponseStatus.ResponseStatus;

                var propertyInfo = this.ResponseDto.GetType().GetProperty("ResponseStatus");
                if (propertyInfo == null)
                    return null;

                return ReflectionUtils.GetProperty(this.ResponseDto, propertyInfo) as ResponseStatus;
            }
        }

        public List<ResponseError> GetFieldErrors()
        {
            var responseStatus = ResponseStatus;
            if (responseStatus != null)
                return responseStatus.Errors ?? new List<ResponseError>();

            return new List<ResponseError>();
        }
    }
}
