using System;
using ServiceStack.Model;

namespace ServiceStack.Messaging
{
    public class MessagingException : Exception, IHasResponseStatus, IResponseStatusConvertible
    {
        public MessagingException() {}

        public MessagingException(string message) : base(message) {}

        public MessagingException(string message, Exception innerException) : base(message, innerException) {}

        public MessagingException(ResponseStatus responseStatus, Exception innerException = null)
            : base(responseStatus.Message ?? responseStatus.ErrorCode, innerException)
        {
            ResponseStatus = responseStatus;
        }

        public MessagingException(ResponseStatus responseStatus, object responseDto, Exception innerException = null)
            : this(responseStatus, innerException)
        {
            ResponseDto = responseDto;
        }

        public object ResponseDto { get; set; }

        public ResponseStatus ResponseStatus { get; set; }

        public ResponseStatus ToResponseStatus()
        {
            return ResponseStatus;
        }
    }
}