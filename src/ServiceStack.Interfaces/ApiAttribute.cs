//Copyright (c) ServiceStack, Inc. All Rights Reserved.
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

        /// <summary>
        /// Create or not body param for request type when verb is POST or PUT. 
        /// If `true` body param is always generated.
        /// If `false` body param is never generated
        /// If `null` (default) body param is generated only if `DisableAutoDtoInBodyParam = false`
        /// </summary>
        public bool? GenerateBodyDtoParam { get; set; }

        /// <summary>
        /// Tells if body param is required
        /// </summary>
        public bool? IsRequired { get; set; }

        public ApiAttribute(string description)
        {
            Description = description;
        }

        public ApiAttribute() { }
    }
}