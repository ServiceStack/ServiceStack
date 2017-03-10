using System.Runtime.Serialization;

namespace ServiceStack.Api.Swagger2.Specification
{
    [DataContract]
    public class Swagger2Tag
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "externalDocs")]
        public Swagger2ExternalDocumentation ExternalDocs { get; set; }
    }
}
