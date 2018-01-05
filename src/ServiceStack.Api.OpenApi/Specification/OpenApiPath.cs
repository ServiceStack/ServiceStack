using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.Api.OpenApi.Specification
{
    [DataContract]
    public class OpenApiPath
    {
        [DataMember(Name = "$ref")]
        public string Ref { get; set; }
        [DataMember(Name = "get")]
        public OpenApiOperation Get { get; set; }
        [DataMember(Name = "put")]
        public OpenApiOperation Put { get; set; }
        [DataMember(Name = "post")]
        public OpenApiOperation Post { get; set; }
        [DataMember(Name = "delete")]
        public OpenApiOperation Delete { get; set; }
        [DataMember(Name = "options")]
        public OpenApiOperation Options { get; set; }
        [DataMember(Name = "head")]
        public OpenApiOperation Head { get; set; }
        [DataMember(Name = "patch")]
        public OpenApiOperation Patch { get; set; }
        [DataMember(Name = "parameters")]
        public List<OpenApiParameter> Parameters { get; set; }
    }

}
