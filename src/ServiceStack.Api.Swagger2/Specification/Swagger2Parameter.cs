using System.Runtime.Serialization;

namespace ServiceStack.Api.Swagger2.Specification
{
    [DataContract]
    public class Swagger2Parameter : Swagger2DataTypeSchema
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "in")]
        public string In { get; set; }
        [DataMember(Name = "description")]
        public string Description { get; set; }
        [DataMember(Name = "required")]
        public bool Required { get; set; }
        [DataMember(Name = "schema")]
        public Swagger2Schema Schema { get; set; }
        [DataMember(Name = "allowEmptyValue")]
        public bool AllowEmptyValue { get; set; }
    }
}
