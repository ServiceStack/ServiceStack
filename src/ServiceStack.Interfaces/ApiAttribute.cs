//Copyright (c) Service Stack LLC. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;

namespace ServiceStack
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ApiAttribute : AttributeBase
    {
        /// <summary>
        /// The overall description of an API. Used by Swagger.
        /// </summary>
        public string Description { get; set; }

        public ApiAttribute(string description)
        {
            Description = description;
        }

        public ApiAttribute() { }
    }
}