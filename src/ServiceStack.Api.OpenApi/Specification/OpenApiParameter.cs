using System.Runtime.Serialization;

namespace ServiceStack.Api.OpenApi.Specification
{
    [DataContract]
    public class OpenApiParameter : OpenApiDataTypeSchema
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "in")]
        public string In { get; set; }
        [DataMember(Name = "schema")]
        public OpenApiSchema Schema { get; set; }
        [DataMember(Name = "allowEmptyValue")]
        public bool? AllowEmptyValue { get; set; }
    }
}
