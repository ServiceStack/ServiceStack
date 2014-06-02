using System;

namespace ServiceStack.Data
{
    public class OptimisticConcurrencyException : DataException
    {
        public OptimisticConcurrencyException() {}

        public OptimisticConcurrencyException(string message) : base(message) {}

        public OptimisticConcurrencyException(string message, Exception innerException) 
            : base(message, innerException) {}
    }
}