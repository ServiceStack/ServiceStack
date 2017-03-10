using System.Runtime.Serialization;

namespace ServiceStack.Api.Swagger2.Specification
{
    [DataContract]
    public class Swagger2ExternalDocumentation
    {
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "url")]
        public string Url { get; set; }
    }
}
