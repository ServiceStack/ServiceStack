using System;

namespace ServiceStack.ServiceHost
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ApiAttribute : Attribute
    {
        /// <summary>
        /// The overall description of an API. Used by Swagger.
        /// </summary>
        public string Description { get; set; }

        public ApiAttribute() {}
        public ApiAttribute(string description)
        {
            Description = description;
        }
    }
}