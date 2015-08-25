// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using ServiceStack.Text;

namespace ServiceStack
{
#if !(NETFX_CORE || WP || SL5 || PCL)
    [Serializable]
#endif
    public class WebServiceException
        : Exception
    {
        public WebServiceException() { }
        public WebServiceException(string message) : base(message) { }
        public WebServiceException(string message, Exception innerException) : base(message, innerException) { }

        public int StatusCode { get; set; }

        public string StatusDescription { get; set; }

        public WebHeaderCollection ResponseHeaders { get; set; }

        public object ResponseDto { get; set; }
        
        public string ResponseBody { get; set; }

        private string errorCode;

        private void ParseResponseDto()
        {
            string responseStatus;
            if (!TryGetResponseStatusFromResponseDto(out responseStatus))
            {
                if (!TryGetResponseStatusFromResponseBody(out responseStatus))
                {
                    errorCode = StatusDescription;
                    return;
                }
            }

            var rsMap = TypeSerializer.DeserializeFromString<Dictionary<string, string>>(responseStatus);
            if (rsMap == null) return;

            rsMap = new Dictionary<string, string>(rsMap, PclExport.Instance.InvariantComparerIgnoreCase);
            rsMap.TryGetValue("ErrorCode", out errorCode);
            rsMap.TryGetValue("Message", out errorMessage);
            rsMap.TryGetValue("StackTrace", out serverStackTrace);
        }

        private bool TryGetResponseStatusFromResponseDto(out string responseStatus)
        {
            responseStatus = String.Empty;
            try
            {
                if (ResponseDto == null)
                    return false;
                var jsv = TypeSerializer.SerializeToString(ResponseDto);
                var map = TypeSerializer.DeserializeFromString<Dictionary<string, string>>(jsv);
                map = new Dictionary<string, string>(map, PclExport.Instance.InvariantComparerIgnoreCase);

                return map.TryGetValue("ResponseStatus", out responseStatus);
            }
            catch
            {
                return false;
            }
        }

        private bool TryGetResponseStatusFromResponseBody(out string responseStatus)
        {
            responseStatus = String.Empty;
            try
            {
                if (String.IsNullOrEmpty(ResponseBody)) return false;
                var map = TypeSerializer.DeserializeFromString<Dictionary<string, string>>(ResponseBody);
                map = new Dictionary<string, string>(map, PclExport.Instance.InvariantComparerIgnoreCase);
                return map.TryGetValue("ResponseStatus", out responseStatus);
            }
            catch
            {
                return false;
            }
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

                var propertyInfo = this.ResponseDto.GetType().GetPropertyInfo("ResponseStatus");
                if (propertyInfo == null)
                    return null;

                return propertyInfo.GetProperty(this.ResponseDto) as ResponseStatus;
            }
        }

        public List<ResponseError> GetFieldErrors()
        {
            var responseStatus = ResponseStatus;
            if (responseStatus != null)
                return responseStatus.Errors ?? new List<ResponseError>();

            return new List<ResponseError>();
        }

        public bool IsAny400()
        {
            return StatusCode >= 400 && StatusCode < 500;
        }

        public bool IsAny500()
        {
            return StatusCode >= 500 && StatusCode < 600;
        }
    }
}
