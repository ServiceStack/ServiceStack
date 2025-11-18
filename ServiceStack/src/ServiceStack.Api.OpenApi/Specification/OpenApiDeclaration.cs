using System.Collections.Generic;
using System.Runtime.Serialization;
using ServiceStack.DataAnnotations;

#if !NET10_0_OR_GREATER
using ServiceStack.Api.OpenApi.Support;
#endif

namespace ServiceStack.Api.OpenApi.Specification
{
    [DataContract]
    [ExcludeMetadata]
    public class OpenApiDeclaration
    {
        [DataMember(Name = "swagger")]
        public string Swagger => "2.0";

        [DataMember(Name = "info")]
        public OpenApiInfo Info { get; set; }

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
        public OrderedDictionary<string, OpenApiPath> Paths { get; set; }

        [DataMember(Name = "definitions")]
        public Dictionary<string, OpenApiSchema> Definitions { get; set; }

        [DataMember(Name = "parameters")]
        public Dictionary<string, OpenApiParameter> Parameters { get; set; }

        [DataMember(Name = "responses")]
        public OrderedDictionary<string, OpenApiResponse> Responses { get; set; }

        [DataMember(Name = "securityDefinitions")]
        public Dictionary<string, OpenApiSecuritySchema> SecurityDefinitions { get; set; }

        [DataMember(Name = "security")]
        public List<Dictionary<string, List<string>>> Security { get; set; }

        [DataMember(Name = "tags")]
        public List<OpenApiTag> Tags { get; set; }

        [DataMember(Name = "externalDocs")]
        public OpenApiExternalDocumentation ExternalDocs { get; set; }
    }
}
