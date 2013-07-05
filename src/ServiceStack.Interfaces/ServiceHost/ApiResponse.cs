using System;
using System.Net;

namespace ServiceStack.ServiceHost
{
    public interface IApiResponseDescription
    {
        /// <summary>
        /// The status code of a response
        /// </summary>
        int StatusCode { get; }

        /// <summary>
        /// The description of a response status code
        /// </summary>
        string Description { get; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ApiResponseAttribute : Attribute, IApiResponseDescription
    {
        public int StatusCode { get; set; }

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