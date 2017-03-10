using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.Api.Swagger2.Specification
{
    [DataContract]
    public class Swagger2Response
    {
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "schema")]
        public Swagger2Schema Schema { get; set; }
        [DataMember(Name = "headers")]
        public Dictionary<string, Swagger2Property> Headers { get; set; }
        [DataMember(Name = "examples")]
        public Dictionary<string, string> Examples { get; set; }
    }
}
