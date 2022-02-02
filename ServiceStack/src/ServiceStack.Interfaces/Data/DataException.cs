//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;

namespace ServiceStack.Data
{
    public class DataException : Exception
    {
        public DataException() { }
        public DataException(string message) : base(message) { }
        public DataException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}