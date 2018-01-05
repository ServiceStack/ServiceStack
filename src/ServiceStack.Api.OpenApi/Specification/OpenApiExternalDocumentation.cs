using System.Runtime.Serialization;

namespace ServiceStack.Api.OpenApi.Specification
{
    [DataContract]
    public class OpenApiExternalDocumentation
    {
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "url")]
        public string Url { get; set; }
    }
}
