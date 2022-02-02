using System;
using ServiceStack.Text;

namespace ServiceStack
{
    public class ServiceResponseException
        : Exception
    {
        public ServiceResponseException() {}

        public ServiceResponseException(string message)
            : base(message) {}

        public ServiceResponseException(string errorCode, string errorMessage)
            : base(GetErrorMessage(errorCode, errorMessage))
        {
            this.ErrorCode = errorCode;
        }

        public ServiceResponseException(string errorCode, string errorMessage, string serviceStackTrace)
            : base(errorMessage)
        {
            this.ErrorCode = errorCode;
            this.ServiceStackTrace = serviceStackTrace;
        }

        public ServiceResponseException(ResponseStatus responseStatus)
            : base(GetErrorMessage(responseStatus.ErrorCode, responseStatus.Message))
        {
            this.ErrorCode = responseStatus.ErrorCode;
        }

        private static string GetErrorMessage(string errorCode, string errorMessage)
        {
            return errorMessage ?? errorCode?.ToEnglish();
        }

        public string ErrorCode { get; set; }

        public string ServiceStackTrace { get; set; }
    }
}