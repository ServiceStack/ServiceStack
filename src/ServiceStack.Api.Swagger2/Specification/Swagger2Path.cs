using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ServiceStack.Api.Swagger2.Specification
{
    [DataContract]
    public class Swagger2Path
    {
        [DataMember(Name = "$ref")]
        public string Ref { get; set; }
        [DataMember(Name = "get")]
        public Swagger2Operation Get { get; set; }
        [DataMember(Name = "put")]
        public Swagger2Operation Put { get; set; }
        [DataMember(Name = "post")]
        public Swagger2Operation Post { get; set; }
        [DataMember(Name = "delete")]
        public Swagger2Operation Delete { get; set; }
        [DataMember(Name = "options")]
        public Swagger2Operation Options { get; set; }
        [DataMember(Name = "head")]
        public Swagger2Operation Head { get; set; }
        [DataMember(Name = "patch")]
        public Swagger2Operation Patch { get; set; }
        [DataMember(Name = "parameters")]
        public List<Swagger2Parameter> Parameters { get; set; }
    }

}
