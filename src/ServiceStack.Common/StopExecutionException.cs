using System;

namespace ServiceStack
{
    public class StopExecutionException : Exception
    {
        public StopExecutionException() {}

        public StopExecutionException(string message) : base(message) {}

        public StopExecutionException(string message, Exception innerException)
            : base(message, innerException) {}
    }
}