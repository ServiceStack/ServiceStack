using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.Api.Swagger2.Support;

namespace ServiceStack.Api.Swagger2.Specification
{
    [DataContract]
    public class Swagger2ApiDeclaration
    {
        [DataMember(Name = "swagger")]
        public string Swagger => "2.0";

        [DataMember(Name = "info")]
        public Swagger2Info Info { get; set; }

        [DataMember(Name = "host")]
        public string Host { get; set; }

        [DataMember(Name = "basePath")]
        public string BasePath { get; set; }

        [DataMember(Name = "schemes")]
        public List<string> Schemes { get; set; }

        [DataMember(Name = "consumes")]
        public List<string> Consumes { get; set; }

        [DataMember(Name = "produces")]
        public List<string> Produces { get; set; }

        [DataMember(Name = "paths")]
        public OrderedDictionary<string, Swagger2Path> Paths { get; set; }

        [DataMember(Name = "definitions")]
        public Dictionary<string, Swagger2Schema> Definitions { get; set; }

        [DataMember(Name = "parameters")]
        public Dictionary<string, Swagger2Parameter> Parameters { get; set; }

        [DataMember(Name = "responses")]
        public OrderedDictionary<string, Swagger2Response> Responses { get; set; }

        [DataMember(Name = "securityDefinitions")]
        public Dictionary<string, Swagger2SecuritySchema> SecurityDefinitions { get; set; }

        [DataMember(Name = "security")]
        public Dictionary<string, List<string>> Security { get; set; }

        [DataMember(Name = "tags")]
        public List<Swagger2Tag> Tags { get; set; }

        [DataMember(Name = "externalDocs")]
        public Swagger2ExternalDocumentation ExternalDocs { get; set; }
    }
}
