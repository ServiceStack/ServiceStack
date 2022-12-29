using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace ServiceStack.Api.OpenApi.Specification
{
    [DataContract]
    public class OpenApiProperty : OpenApiDataTypeSchema
    {
        [IgnoreDataMember]
        public PropertyInfo PropertyInfo { get; set; }
        [IgnoreDataMember]
        public Type PropertyType { get; set; }
    }
}
