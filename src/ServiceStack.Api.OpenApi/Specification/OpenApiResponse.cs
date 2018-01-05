using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.Api.OpenApi.Specification
{
    [DataContract]
    public class OpenApiResponse
    {
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "schema")]
        public OpenApiSchema Schema { get; set; }
        [DataMember(Name = "headers")]
        public Dictionary<string, OpenApiProperty> Headers { get; set; }
        [DataMember(Name = "examples")]
        public Dictionary<string, string> Examples { get; set; }
    }
}
