using System.Runtime.Serialization;

namespace ServiceStack.Api.OpenApi.Specification
{
    [DataContract]
    public class OpenApiXmlObject
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "namespace")]
        public string Namespace { get; set; }
        [DataMember(Name = "prefix")]
        public string Prefix { get; set; }
        [DataMember(Name = "attribute")]
        public bool Attribute { get; set; }
        [DataMember(Name = "wrapped")]
        public bool Wrapped { get; set; }
    }

}
