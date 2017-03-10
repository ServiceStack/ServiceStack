using System.Runtime.Serialization;

namespace ServiceStack.Api.Swagger2.Specification
{
    [DataContract]
    public class Swagger2Contact
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "url")]
        public string Url { get; set; }
        [DataMember(Name = "email")]
        public string Email { get; set; }
    }
}
