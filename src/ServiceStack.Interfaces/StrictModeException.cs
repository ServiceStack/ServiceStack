using System;

namespace ServiceStack
{
    /// <summary>
    /// Additional checks to notify of invalid state, configuration or use of ServiceStack libraries.
    /// Can disable StrictMode checks with Config.StrictMode = false;
    /// </summary>
    public class StrictModeException : ArgumentException
    {
        public string Code { get; set; }

        public StrictModeException() {}

        public StrictModeException(string message, string code = null) 
            : base(message)
        {
            Code = code;
        }

        public StrictModeException(string message, Exception innerException, string code = null) 
            : base(message, innerException)
        {
            Code = code;
        }

        public StrictModeException(string message, string paramName, string code = null)
            : base(message, paramName)
        {
            Code = code;
        }
    }
}