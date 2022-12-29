using System.Runtime.Serialization;

namespace ServiceStack.Api.OpenApi.Specification
{
    [DataContract]
    public class OpenApiInfo
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "termsOfServiceUrl")]
        public string TermsOfServiceUrl { get; set; }
        [DataMember(Name = "contact")]
        public OpenApiContact Contact { get; set; }
        [DataMember(Name = "license")]
        public OpenApiLicense License { get; set; }
        [DataMember(Name = "version")]
        public string Version { get; set; }
    }
}
