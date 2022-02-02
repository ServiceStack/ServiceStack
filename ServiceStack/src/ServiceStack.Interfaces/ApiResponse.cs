//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Net;

namespace ServiceStack
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
    public class ApiResponseAttribute : AttributeBase, IApiResponseDescription
    {
        /// <summary>
        /// HTTP status code of response
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// End-user description of the data which is returned by response
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// If set to true, the response is default for all non-explicitly defined status codes 
        /// </summary>
        public bool IsDefaultResponse { get; set; }

        /// <summary>
        /// Open API schema definition type for response
        /// </summary>
        public Type ResponseType { get; set; }

        public ApiResponseAttribute() { }

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