using ServiceStack.Api.OpenApi.Support;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.Api.OpenApi.Specification
{
    [DataContract]
    public class OpenApiOperation
    {
        //Custom: Request DTO Name to help with custom filtering
        [IgnoreDataMember]
        public string RequestType { get; set; } 

        [DataMember(Name = "tags")]
        public List<string> Tags { get; set; }
        [DataMember(Name = "summary")]
        public string Summary { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "externalDocs")]
        public OpenApiExternalDocumentation ExternalDocs { get; set; }
        [DataMember(Name = "operationId")]
        public string OperationId { get; set; }
        [DataMember(Name = "consumes")]
        public List<string> Consumes { get; set; }
        [DataMember(Name = "produces")]
        public List<string> Produces { get; set; }
        [DataMember(Name = "parameters")]
        public List<OpenApiParameter> Parameters { get; set; }
        [DataMember(Name = "responses")]
        public OrderedDictionary<string, OpenApiResponse> Responses { get; set; }
        [DataMember(Name = "schemes")]
        public List<string> Schemes { get; set; }
        [DataMember(Name = "deprecated")]
        public bool Deprecated { get; set; }
        [DataMember(Name = "security")]
        public List<Dictionary<string, List<string>>> Security { get; set; }
    }

}
