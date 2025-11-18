using System.Collections.Generic;
using System.Runtime.Serialization;

#if !NET10_0_OR_GREATER
using ServiceStack.Api.OpenApi.Support;
#endif

namespace ServiceStack.Api.OpenApi.Specification
{
    [DataContract]
    public class OpenApiSchema : OpenApiDataTypeSchema
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }
        [DataMember(Name = "discriminator")]
        public string Discriminator { get; set; }
        [DataMember(Name = "readOnly")]
        public bool? ReadOnly { get; set; }
        [DataMember(Name = "xml")]
        public OpenApiXmlObject Xml { get; set; }
        [DataMember(Name = "externalDocs")]
        public OpenApiExternalDocumentation ExternalDocs { get; set; }
        [DataMember(Name = "example")]
        public string Example { get; set; }

        [DataMember(Name = "required")]
        public new List<string> Required { get; set; }

        [DataMember(Name = "allOf")]
        public OpenApiSchema AllOf { get; set; }
        [DataMember(Name = "properties")]
        public OrderedDictionary<string, OpenApiProperty> Properties { get; set; }
        [DataMember(Name = "additionalProperties")]
        public OpenApiProperty AdditionalProperties { get; set; }
    }
}
