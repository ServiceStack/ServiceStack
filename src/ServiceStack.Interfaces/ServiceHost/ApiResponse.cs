using System;
using System.Net;

namespace ServiceStack.ServiceHost
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ApiResponseAttribute : Attribute
    {
        /// <summary>
        /// The status code of a response
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// The description of a response status code
        /// </summary>
        public string Description { get; set; }

        public ApiResponseAttribute(HttpStatusCode statusCode, string description)
        {
            StatusCode = (int)statusCode;
            Description = description;
        }

        public ApiResponseAttribute(int statusCode, string description)
        {
            StatusCode = statusCode;
            Description = description;
        }
    }
}