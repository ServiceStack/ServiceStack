using System.Runtime.Serialization;

namespace ServiceStack.Api.Swagger2.Specification
{
    [DataContract]
    public class Swagger2Property : Swagger2DataTypeSchema
    {
        [DataMember(Name = "$ref")]
        public string Ref { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
    }
}
