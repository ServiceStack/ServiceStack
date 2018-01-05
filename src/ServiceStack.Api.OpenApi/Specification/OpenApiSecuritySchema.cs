using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.Api.OpenApi.Specification
{
    [DataContract]
    public class OpenApiSecuritySchema
    {
        [DataMember(Name = "type")]
        public string Type { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "in")]
        public string In { get; set; }
        [DataMember(Name = "flow")]
        public string Flow { get; set; }
        [DataMember(Name = "authorizationUrl")]
        public string AuthorizationUrl { get; set; }
        [DataMember(Name = "tokenUrl")]
        public string TokenUrl { get; set; }
        [DataMember(Name = "scopes")]
        public Dictionary<string, string> Scopes { get; set; }
    }
}
