using System;

namespace ServiceStack.ServiceHost
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class Api : Attribute
    {
        /// <summary>
        /// The overall description of an API. Used by Swagger.
        /// </summary>
        public string Description { get; set; }

        public Api() {}
        public Api(string description)
        {
            Description = description;
        }
    }
}