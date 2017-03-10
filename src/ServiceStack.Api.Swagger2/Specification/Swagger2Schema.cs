using ServiceStack.Api.Swagger2.Support;
using System.Runtime.Serialization;

namespace ServiceStack.Api.Swagger2.Specification
{
    [DataContract]
    public class Swagger2Schema : Swagger2DataTypeSchema
    {
        [DataMember(Name = "$ref")]
        public string Ref { get; set; }
        [DataMember(Name = "title")]
        public string Title { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "discriminator")]
        public string Discriminator { get; set; }
        [DataMember(Name = "readOnly")]
        public bool? ReadOnly { get; set; }
        [DataMember(Name = "xml")]
        public Swagger2XmlObject Xml { get; set; }
        [DataMember(Name = "externalDocs")]
        public Swagger2ExternalDocumentation ExternalDocs { get; set; }
        [DataMember(Name = "example")]
        public string Example { get; set; }

        [DataMember(Name = "allOf")]
        public Swagger2Schema AllOf { get; set; }
        [DataMember(Name = "properties")]
        public OrderedDictionary<string, Swagger2Property> Properties { get; set; }
        [DataMember(Name = "additionalProperties")]
        public OrderedDictionary<string, Swagger2Property> AdditionalProperties { get; set; }
    }
}
