using System;

namespace ServiceStack.Api.Swagger
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SwaggerAllowableValuesAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets parameter name with which allowable values will be associated.
        /// </summary>
        public string Name { get; set; }

        //TODO: should be implemented according to:
        //https://github.com/wordnik/swagger-core/wiki/datatypes
    }
}
