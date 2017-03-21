using System.Runtime.Serialization;

namespace ServiceStack.Api.OpenApi.Specification
{
    [DataContract]
    public class OpenApiLicense
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "url")]
        public string Url { get; set; }
    }
}
