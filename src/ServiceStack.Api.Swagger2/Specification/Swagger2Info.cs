using System.Runtime.Serialization;

namespace ServiceStack.Api.Swagger2.Specification
{
    [DataContract]
    public class Swagger2Info
    {
        [DataMember(Name = "title")]
        public string Title { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "termsOfServiceUrl")]
        public string TermsOfServiceUrl { get; set; }
        [DataMember(Name = "contact")]
        public Swagger2Contact Contact { get; set; }
        [DataMember(Name = "license")]
        public Swagger2License License { get; set; }
        [DataMember(Name = "version")]
        public string Version { get; set; }
    }
}
