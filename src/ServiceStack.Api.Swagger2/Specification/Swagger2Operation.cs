using ServiceStack.Api.Swagger2.Support;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.Api.Swagger2.Specification
{
    [DataContract]
    public class Swagger2Operation
    {
        [DataMember(Name = "tags")]
        public List<string> Tags { get; set; }
        [DataMember(Name = "summary")]
        public string Summary { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "externalDocs")]
        public Swagger2ExternalDocumentation ExternalDocs { get; set; }
        [DataMember(Name = "operationId")]
        public string OperationId { get; set; }
        [DataMember(Name = "consumes")]
        public List<string> Consumes { get; set; }
        [DataMember(Name = "produces")]
        public List<string> Produces { get; set; }
        [DataMember(Name = "parameters")]
        public List<Swagger2Parameter> Parameters { get; set; }
        [DataMember(Name = "responses")]
        public OrderedDictionary<string, Swagger2Response> Responses { get; set; }
        [DataMember(Name = "schemes")]
        public List<string> Schemes { get; set; }
        [DataMember(Name = "deprecated")]
        public bool Deprecated { get; set; }
        [DataMember(Name = "security")]
        public Dictionary<string, List<string>> Security { get; set; }
    }

}
